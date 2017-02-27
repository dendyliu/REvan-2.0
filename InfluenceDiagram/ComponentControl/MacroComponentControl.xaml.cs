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
using System.Windows.Navigation;
using System.Windows.Shapes;
using InfluenceDiagram.Data;
using InfluenceDiagram.Utility;
using DiagramDesigner;

namespace InfluenceDiagram.ComponentControl
{
    /// <summary>
    /// Interaction logic for MacroComponentControl.xaml
    /// </summary>
    public partial class MacroComponentControl : UserControl, IComponentControl
    {
        public MacroComponentData data {get; private set;}
        public RootComponentData rootData { get { return data; } }
        private IComponentValueStore valueStore;

        public IConnectable GetConnector(string variableId)
        {
            if (variableId == data.id)
            {
                return textExpression;
            }
            else
            {
                var obj = data.parametersData.SingleOrDefault(o => o.id == variableId);
                int index = data.parametersData.IndexOf(obj);
                //int index = data.parametersData.FindIndex(o => o.id == variableId);
                if (index >= 0)
                {
                    return UIHelper.FindVisualChild<MacroComponentTextParam>(containerParams.ItemContainerGenerator.ContainerFromIndex(index));
                }
            }
            return null;
        }

        public MacroComponentControl(MacroComponentData data, IComponentValueStore valueStore)
        {
            this.data = data;
            this.valueStore = valueStore;
            InitializeComponent();
            this.Loaded += MacroComponentControl_Loaded;
            TextBoxMasking.SetMask(textName, DataHelper.FunctionNameRegex);
            textExpression.Document.TextAlignment = TextAlignment.Center;
            textExpression.valueStore = valueStore;
            textExpression.data = data.expressionData;
            textName.Text = data.Function;
            containerParams.ItemsSource = data.parametersData;

        }

        void MacroComponentControl_Loaded(object sender, RoutedEventArgs e)
        {
            DependencyObject parent = this.Parent as DependencyObject;
            data.BindPositionToCanvas(parent);
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            textName.Text = data.Function;                  
            //containerParams.Items.Refresh();
            textExpression.UpdateDisplay();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            DependencyObject textBox = sender as DependencyObject;
            if (e.Key == Key.Return)
            {
                /*DependencyObject scope = FocusManager.GetFocusScope(this);
                FocusManager.SetFocusedElement(scope, Application.Current.MainWindow);
                Keyboard.Focus(Application.Current.MainWindow);*/
                RoutedUICommand command = Command.UnselectComponent;
                command.Execute(sender, Application.Current.MainWindow);
            }
        }

        private void textName_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string newName = textBox.Text;
            if (newName != this.data.Function)
            {
                if (newName.Length > 0)
                {
                    try
                    {
                        Command.MacroChangeName.Execute(textName.Text, this);
                    }
                    catch (DataException exc)
                    {
                        textBox.Text = this.data.Function;
                        MessageBox.Show(exc.Message);
                    }
                }
                else
                {
                    UpdateDisplay();
                }
            }
        }

        private void TextParam_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textParam = sender as TextBox;
            int index = data.parametersData.IndexOf(textParam.Tag as MacroParameterData);

            RoutedUICommand command = Command.ActivateComponent;
            command.Execute(sender, Application.Current.MainWindow);
            // calling UpdateDisplay will recreate the parameter TextBoxes!
            //UpdateDisplay();
        }

        private void TextParam_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textParam = sender as TextBox;
            int index = data.parametersData.IndexOf(textParam.Tag as MacroParameterData);
            try
            {
                data.RenameParameter(index, textParam.Text);

                RoutedUICommand command = Command.DeactivateComponent;
                command.Execute(sender, Application.Current.MainWindow);
                UpdateDisplay();
            }
            catch (DataException exc)
            {
                textParam.Text = data.parametersData[index].varname;
                MessageBox.Show(exc.Message);
            }
        }

        private void UpdateExpressionData()
        {
            string newExpression = textExpression.GetExpression();
            //if (newExpression != this.data.expression)
            //{
                this.data.expression = newExpression;
            //}
        }

        private void textExpression_GotFocus(object sender, RoutedEventArgs e)
        {
            RoutedUICommand command = Command.ActivateComponent;
            command.Execute(sender, Application.Current.MainWindow);
            UpdateDisplay();
        }

        private void textExpression_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateExpressionData();
            RoutedUICommand command = Command.DeactivateComponent;
            command.Execute(sender, Application.Current.MainWindow);
            UpdateDisplay();
        }

        private void textName_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            BorderDecorator.DecorateBorderHexagon(borderName, textName);
        }
    }

    public class MacroComponentTextExpression: ExpressionTextBox, IComponentVariableReceiver, IComponentVariableSource
    {
        public MacroExpressionData data;

        public MacroComponentTextExpression(): base()
        {
        }

        public void UpdateDisplay()
        {
            if (this.IsFocused)
            {
                this.SetExpression(data.expression);
            }
            else
            {
                this.SetPlainText(data.GetValueAsString());
            }
        }

        public bool ReceiveComponentVariable(IComponentVariableSource component)
        {
            if (!(component.GetData() is MacroParameterData))
            {
                string parentId = valueStore.GetRootComponentId(data.id);
                string otherParentId = valueStore.GetRootComponentId(component.GetData().id);
                if (parentId == otherParentId) return false;
            }
            return this.ReceiveComponentVariable(component, data);
        }

        // macro expression can only receive its own parameter
        override protected bool CheckParameterData(AbstractParameterData receivedData)
        {
            return receivedData.parentId == data.parentId;
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (!this.IsFocused)
            {
                RoutedUICommand command = Command.ClickComponent;
                command.Execute(this, Application.Current.MainWindow);
                e.Handled = true;
            }
        }

        public AbstractComponentData GetData()
        {
            // return the MacroComponentData, not MacroExpressionData
            return (UIHelper.FindVisualParent<MacroComponentControl>(this) as MacroComponentControl).data;
        }
    }

    public class MacroComponentTextParam: TextBox, IComponentVariableSource
    {
        public ControlConnector connector { get; private set; }

        public MacroComponentTextParam()
            : base()
        {
            connector = new ControlConnector();
            connector.HorizontalAlignment = HorizontalAlignment.Center;
            connector.VerticalAlignment = VerticalAlignment.Center;
            this.AddVisualChild(connector);
            connector.InitConnectable(this);

            this.Unloaded += MacroComponentTextParam_Unloaded;
        }

        void MacroComponentTextParam_Unloaded(object sender, RoutedEventArgs e)
        {
            connector.Cleanup();
        }

        public AbstractComponentData GetData()
        {
            return (this.Tag as MacroParameterData);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (!this.IsFocused)
            {
                RoutedUICommand command = Command.ClickComponent;
                command.Execute(this, Application.Current.MainWindow);
                e.Handled = true;
            }
        }
    }
}
