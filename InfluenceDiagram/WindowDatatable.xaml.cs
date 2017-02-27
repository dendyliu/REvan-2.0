using InfluenceDiagram.Data;
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

namespace InfluenceDiagram
{
    /// <summary>
    /// Interaction logic for WindowDatatable.xaml
    /// </summary>
    public partial class WindowDatatable : Window, IComponentVariableReceiver
    {
        MainWindow mainWindow;
        WorksheetData worksheetData;
        SpreadsheetRangeData rangeData;
        IExpressionData rowData, columnData;

        public WindowDatatable(WorksheetData worksheetData, SpreadsheetRangeData rangeData)
        {
            this.worksheetData = worksheetData;
            this.rangeData = rangeData;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application curApp = Application.Current;
            mainWindow = curApp.MainWindow as MainWindow;
            this.Left = mainWindow.Left + (mainWindow.Width - this.ActualWidth) / 2;
            this.Top = mainWindow.Top + (mainWindow.Height - this.ActualHeight) / 2;

            mainWindow.IsPanelsEnabled = false;
            textRow.Focus();
        }

        public bool ReceiveComponentVariable(IComponentVariableSource component)
        {
            this.Focus();

            AbstractComponentData receivedData = component.GetData();
            if (receivedData == null) return false;

            if (receivedData is IExpressionData)
            {
                if (textRow.IsFocused)
                {
                    rowData = receivedData as IExpressionData;
                    textRow.Text = receivedData.autoLabel;
                }
                else if (textColumn.IsFocused)
                {
                    columnData = receivedData as IExpressionData;
                    textColumn.Text = receivedData.autoLabel;
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
            if (rowData != null || columnData != null)
            {
                // must check that the row & column data is not part of rangeData. Otherwise cyclic dependency
                // exception: it's ok when only 1 of row/column data is set and it refers to the top left cell in rangeData, 
                // because the top left cell is free (not linked) unless both row&column data is set
                if (rowData == null || columnData == null)
                {
                    // only 1 data is set, the top left cell on rangeData can be used
                    IExpressionData data = (rowData != null) ? rowData : columnData;
                    if (data is SpreadsheetCellData)
                    {
                        PointInt? position = rangeData.GetPositionOfCell(data as SpreadsheetCellData);
                        if (position.HasValue && !(position.Value.X == 0 && position.Value.Y == 0)){
                            if (rowData != null) 
                                textRow.Focus(); 
                            else 
                                textColumn.Focus();
                            MessageBox.Show("Invalid input. Cannot use a cell inside the selected range!");
                            return false;
                        }
                    }
                }
                else
                {
                    // row data and column data cannot refer to the same thing
                    if ((rowData as AbstractComponentData).id == (columnData as AbstractComponentData).id)
                    {
                        textColumn.Focus();
                        MessageBox.Show("Row input and column input must be different!");
                        return false;
                    }
                    // both data are set, cannot use entire rangeData
                    IExpressionData[] array = {rowData, columnData};
                    foreach (IExpressionData data in array){
                        if (data is SpreadsheetCellData)
                        {
                            PointInt? position = rangeData.GetPositionOfCell(data as SpreadsheetCellData);
                            if (position.HasValue)
                            {
                                if (data == rowData)
                                    textRow.Focus();
                                else
                                    textColumn.Focus();
                                MessageBox.Show("Invalid input. Cannot use a cell inside the selected range!");
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            else
            {
                textRow.Focus();
                MessageBox.Show("Please input the row or column expression");
                return false;
            }
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                mainWindow.CreateDatatable(rangeData, rowData as AbstractComponentData, columnData as AbstractComponentData);
                this.Close();
            }
        }

        private void buttonClearRow_Click(object sender, RoutedEventArgs e)
        {
            rowData = null;
            textRow.Text = "";
        }

        private void buttonClearColumn_Click(object sender, RoutedEventArgs e)
        {
            columnData = null;
            textColumn.Text = "";
        }
    }
}
