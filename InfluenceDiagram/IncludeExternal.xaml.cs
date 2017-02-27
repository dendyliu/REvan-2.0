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
using InfluenceDiagram.Data;
using System.IO;

namespace InfluenceDiagram
{
    /// <summary>
    /// Interaction logic for IncludeExternal.xaml
    /// </summary>
    public partial class IncludeExternal : Window
    {
        private WorksheetData worksheetData;
        
        public IncludeExternal(WorksheetData worksheetData)
        {
            this.worksheetData = worksheetData;
            InitializeComponent();
            listBox.ItemsSource = worksheetData.listExternalWorksheetPaths;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application curApp = Application.Current;
            Window mainWindow = curApp.MainWindow;
            this.Left = mainWindow.Left + (mainWindow.Width - this.ActualWidth) / 2;
            this.Top = mainWindow.Top + (mainWindow.Height - this.ActualHeight) / 2;
        }

        private void Add_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openDialog = new System.Windows.Forms.OpenFileDialog();
            openDialog.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
            openDialog.Filter = "REvan Influence Diagram file|*.rvn";

            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    worksheetData.AddExternalWorksheet(openDialog.FileName);
                }
                catch (Exception exc)
                {
                    MessageBox.Show("Error loading external worksheet\n"+exc.Message);
                }
            }
            e.Handled = true;
        }

        private void Remove_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            worksheetData.RemoveExternalWorksheet(listBox.SelectedIndex);
            e.Handled = true;
        }

        private void Remove_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (listBox != null)
            {
                e.CanExecute = listBox.SelectedItem != null;
            }
            e.Handled = true;
        }
    }
}
