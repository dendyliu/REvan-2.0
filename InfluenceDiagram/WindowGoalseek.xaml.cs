using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using InfluenceDiagram.ComponentControl;
using InfluenceDiagram.Data;
using System.Text.RegularExpressions;
using InfluenceDiagram.Utility;
using System.ComponentModel;
using System.Windows.Threading;
using System.IO;

namespace InfluenceDiagram
{
    /// <summary>
    /// Interaction logic for WindowGoalseek.xaml
    /// </summary>
    /// 
    
    public partial class WindowGoalseek : Window, IComponentVariableReceiver
    {
        private MainWindow mainWindow;
        WorksheetData worksheetData;
        IExpressionData targetData, variableData;

        public WindowGoalseek(WorksheetData worksheetData)
        {
            this.worksheetData = worksheetData;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application curApp = Application.Current;
            mainWindow = curApp.MainWindow as MainWindow;
            this.Left = mainWindow.Left + (mainWindow.Width - this.ActualWidth) / 2;
            this.Top = mainWindow.Top + (mainWindow.Height - this.ActualHeight) / 2;

            TextBoxMasking.SetMask(textValue, DataHelper.NumericRegex);
            mainWindow.IsPanelsEnabled = false;
            textTarget.Focus();
        }

        private void textTarget_GotFocus(object sender, RoutedEventArgs e)
        {
            mainWindow.scrollViewer.IsEnabled = true;
        }

        private void textValue_GotFocus(object sender, RoutedEventArgs e)
        {
            // cannot click canvas when textValue is focused
            mainWindow.scrollViewer.IsEnabled = false;
        }

        private void textVariable_GotFocus(object sender, RoutedEventArgs e)
        {
            mainWindow.scrollViewer.IsEnabled = true;
        }

        public bool ReceiveComponentVariable(IComponentVariableSource component)
        {
            this.Focus();

            AbstractComponentData receivedData = component.GetData();
            if (receivedData == null) return false;
            
            if (receivedData is IExpressionData)
            {
                if (textTarget.IsFocused)
                {
                    targetData = receivedData as IExpressionData;
                    textTarget.Text = receivedData.autoLabel;
                }
                else if (textVariable.IsFocused)
                {
                    variableData = receivedData as IExpressionData;
                    textVariable.Text = receivedData.autoLabel;
                }

                return true;
            }
            else
            {
                // only receive IExpressionData, other type of data cannot have value
                return false;
            }
        }

        bool ValidateInput()
        {
            // validation: 
            // - targetData contains [variable]
            // - value is a number
            // - variableData is a number
            if (targetData == null)
            {
                textTarget.Focus();
                MessageBox.Show(this, "Please select an expression!");
                return false;
            }
            else
            {
                MatchCollection matches = new Regex(DataHelper.VariableRegex).Matches(targetData.expression);
                if (matches.Count == 0)
                {
                    textTarget.Focus();
                    MessageBox.Show(this, "Expression must contain a variable!");
                    return false;
                }
            }
            if (variableData == null)
            {
                textVariable.Focus();
                MessageBox.Show(this, "Please select an expression!");
                return false;
            }
            else
            {
                // if the expression is empty, it is considered as 0
                if (variableData.expression.Length > 0)
                {
                    try
                    {
                        Convert.ToDouble(variableData.expression);
                    }
                    catch (Exception exc)
                    {
                        textVariable.Focus();
                        MessageBox.Show(this, "Expression must contain a number!");
                        return false;
                    }
                }
            }
            if (textValue.Text.Length == 0)
            {
                textValue.Focus();
                MessageBox.Show(this, "Please input a number!");
                return false;
            }
            else
            {
                try
                {
                    Convert.ToDouble(textValue.Text);
                }
                catch (Exception exc)
                {
                    textValue.Focus();
                    MessageBox.Show(this, "Value must be a number!");
                    return false;
                }
            }

            return true;
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            bool DEBUG_GOALSEEK = false;
            TextWriter consoleOut = Console.Out;
            TextWriter consoleErr = Console.Error;
            StreamWriter sw = null, sw2 = null;
            if (DEBUG_GOALSEEK)
            {
                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "log.txt");
                FileStream fs = new FileStream(path, FileMode.Create);
                sw = new StreamWriter(fs);
                Console.SetOut(sw);
                path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "err.txt");
                sw2 = new StreamWriter(new FileStream(path, FileMode.Create));
                Console.SetError(sw2);
            }

            Console.WriteLine("Validate Goalseek");

            if (ValidateInput())
            {
                Console.WriteLine("Goalseek Begin");

                double targetValue = Convert.ToDouble(textValue.Text);
                this.IsEnabled = false;
                panelWait.Visibility = Visibility.Visible;

                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += delegate(object s, DoWorkEventArgs a)
                {
                    try
                    {
                        worksheetData.Goalseek(targetData, targetValue, variableData, delegate(int iteration)
                        {
                            Dispatcher.BeginInvoke(DispatcherPriority.Render,
                             new Action(delegate()
                             {
                                 textIteration.Content = iteration.ToString();
                             }));
                        });
                    }
                    catch (Exception exc)
                    {
                        Dispatcher.BeginInvoke(DispatcherPriority.Render,
                         new Action(delegate()
                         {
                             MessageBox.Show(this, exc.Message);
                         }));
                    }
                };
                worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs a)
                {
                    if (DEBUG_GOALSEEK)
                    {
                        Console.SetOut(consoleOut);
                        Console.SetError(consoleErr);
                        sw.Close();
                        sw2.Close();
                    }

                    this.IsEnabled = true;
                    this.Close();
                };
                worker.RunWorkerAsync();
            }
        }

        private void buttonClearTarget_Click(object sender, RoutedEventArgs e)
        {
            targetData = null;
            textTarget.Text = "";
        }

        private void buttonClearVariable_Click(object sender, RoutedEventArgs e)
        {
            variableData = null;
            textVariable.Text = "";
        }
    }
}
