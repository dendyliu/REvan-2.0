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
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DiagramDesigner;

namespace InfluenceDiagram.ComponentControl
{
    class SpreadsheetDataSource
    {
        public SpreadsheetComponentData data { get; private set; }

        ObservableCollection<List<SpreadsheetCellData>> Values
        {
            get { return data.cells; }
        }

        public SpreadsheetDataSource(SpreadsheetComponentData data)
        {
            this.data = data;
        }
    }

    /// <summary>
    /// Interaction logic for SpreadsheetComponentControl.xaml
    /// </summary>
    public partial class SpreadsheetComponentControl : UserControl, IComponentControl, IComponentVariableSource, IConnectable
    {
        public SpreadsheetComponentData data {get; private set;}
        public RootComponentData rootData { get { return data; } }
        IComponentValueStore valueStore;

        public IConnectable GetConnector(string variableId)
        {
            if (DataHelper.IsSpreadsheetRangeId(variableId))
            {
                return this;
            }
            else
            {
                string[] components = variableId.Split('_');
                if (components.Length == 3)
                {
                    // cell
                    SpreadsheetComponentCell cell = GetComponentCell(variableId);
                    if (cell != null)
                    {
                        return cell.textBox;
                    }
                }
                else if (components.Length == 2)
                {
                    // column / row header
                    if (components[1][0] == 'c')
                    {
                        // column header
                        DataGridColumnHeader header = GetColumnHeader(variableId);
                        if (header != null)
                        {
                            return UIHelper.FindVisualChild<SpreadsheetHeaderExpressionText>(header);
                        }
                    }
                    else if (components[1][0] == 'r')
                    {
                        // row header
                        DataGridRowHeader header = GetRowHeader(variableId);
                        if (header != null)
                        {
                            return UIHelper.FindVisualChild<SpreadsheetHeaderExpressionText>(header);
                        }
                    }
                }
            }

            return null;
        }

        public ControlConnector connector { get; private set; }

        public SpreadsheetComponentControl(SpreadsheetComponentData data, IComponentValueStore valueStore)
        {
            InitializeComponent();
            this.Loaded += SpreadsheetComponentControl_Loaded;
            this.Unloaded += SpreadsheetComponentControl_Unloaded;
            this.data = data;
            this.valueStore = valueStore;            
            this.grid.ItemsSource = data.cells;
            SetupColumns();

            connector = new ControlConnector();
            connector.HorizontalAlignment = HorizontalAlignment.Center;
            connector.VerticalAlignment = VerticalAlignment.Center;
            this.AddVisualChild(connector);
            connector.InitConnectable(this);
        }

        void SpreadsheetComponentControl_Loaded(object sender, RoutedEventArgs e)
        {
            DesignerItem parent = this.Parent as DesignerItem;
            data.BindPositionToCanvas(parent);
            data.PropertyChanged += data_PropertyChanged;

            // add Export Excel context menu to DesignerItem
            List<MenuItem> menuItems = new List<MenuItem>();
            MenuItem exportItem = new MenuItem(){
                Header = "Export to Excel",
                Command = Command.ExportExcel
            };
            menuItems.Add(exportItem);
            parent.AddContextMenuItems(menuItems);

            grid.UpdateLayout();
            for (int row = 0; row < this.grid.Items.Count; ++row)
            {
                UpdateRow(row);
            }

            UpdateDisplay();
        }

        void SpreadsheetComponentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            data.PropertyChanged -= data_PropertyChanged;

            connector.Cleanup();
        }

        void data_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "label")
            {
                UpdateDisplay();
            }
        }

        private void textLabel_LostFocus(object sender, RoutedEventArgs e)
        {
            data.label = textLabel.Text;
        }

        void UpdateDisplay()
        {
            textLabel.Text = data.label;
        }

        void SetupColumns()
        {
            UpdateColumn(0);
            for (int col = 1; col < data.columnDatas.Count; ++col)
            {
                AddColumn(col);
                UpdateColumn(col);
            }
            //grid.Items.Refresh();
        }

        string GetColumnName(int col)
        {
            return SpreadsheetComponentData.GetDefaultColumnName(col);
        }

        public void AddColumn(int? col)
        {
            int colIndex = (col.HasValue ? col.Value : (grid.Columns.Count ));
            SpreadsheetColumn column = new SpreadsheetColumn()
            {
                Binding = new Binding("[" + colIndex + "]"),
                Header = GetColumnName(colIndex),
                HeaderStyle = (Style)this.Resources["HeaderStyle"]
            };
            grid.Columns.Insert(colIndex, column);
            // update the columns binding and header
            for (int i = colIndex+1; i < grid.Columns.Count; ++i)
            {
                UpdateColumn(i);
            }
            //grid.Items.Refresh();
        }

        public void AddRow(int? row)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render,
             new Action(delegate()
             {
                 //grid.Items.Refresh();            
                 for (int i = 0; i < grid.Items.Count; ++i)
                 {
                     UpdateRow(i);
                 }
             }));
        }

        void UpdateRow(int row)
        {
            DataGridRow gridRow = grid.GetRow(row);
            if (gridRow == null) return;
            if (data.rowDatas[row].HasCustomLabel())
            {
                gridRow.Header = data.rowDatas[row].label;                
            }
            else
            {
                gridRow.Header = "";
                gridRow.Header = GetRowName(row);
            }
            for (int col = 0; col < grid.Columns.Count; col++)
            {
                DataGridCell cell = grid.GetCell(row, col);
                if (cell != null)
                {
                    SpreadsheetComponentCell component = cell.Content as SpreadsheetComponentCell;
                    component.UpdateDisplay();
                }
            }
            gridRow.Tag = data.rowDatas[row].HasExpression();        // use Tag to store IsReadOnly information
        }

        string GetRowName(int row)
        {
            return SpreadsheetComponentData.GetDefaultRowName(row);
        }

        SpreadsheetComponentCell GetComponentCell(string variableId)
        {
            string[] components = variableId.Split('_');
            if (components.Length != 3) return null;

            string rowId = components[1];
            string colId = components[2];
            int row = data.GetRowIndexFromRowId(rowId);
            int col = data.GetColumnIndexFromColumnId(colId);
            if (col >= 0)
            {
                DataGridCell cell = grid.GetCell(row, col);
                if (cell != null)
                {
                    return cell.Content as SpreadsheetComponentCell;
                }
            }
            return null;
        }

        DataGridColumnHeader GetColumnHeader(string variableId)
        {
            string[] components = variableId.Split('_');
            string colId = components[1].Substring(1);
            int col = data.GetColumnIndexFromColumnId(colId);
            if (col >= 0)
            {
                return GetColumnHeader(col);
            }
            return null;
        }

        DataGridColumnHeader GetColumnHeader(int col)
        {
            List<DataGridColumnHeader> headers = UIHelper.FindVisualChildren<DataGridColumnHeader>(grid).ToList();
            foreach (DataGridColumnHeader header in headers)
            {
                if (header.Column == grid.Columns[col])
                {
                    return header;
                }
            }
            return null;
        }

        DataGridRowHeader GetRowHeader(string variableId)
        {
            string[] components = variableId.Split('_');
            string rowId = components[1].Substring(1);
            int row = data.GetRowIndexFromRowId(rowId);
            if (row >= 0)
            {
                return GetRowHeader(row);
            }
            return null;
        }

        DataGridRowHeader GetRowHeader(int row)
        {
            List<DataGridRowHeader> headers = UIHelper.FindVisualChildren<DataGridRowHeader>(grid).ToList();
            foreach (DataGridRowHeader header in headers)
            {
                DataGridRow gridRow = UIHelper.FindVisualParent<DataGridRow>(header);
                int rowIndex = (gridRow as DataGridRow).GetIndex();
                if (row == rowIndex)
                {
                    return header;
                }
            }
            return null;
        }

        public void UpdateDisplay(string variableId)
        {
            string[] components = variableId.Split('_');
            if (components.Length == 3)
            {
                SpreadsheetComponentCell cell = GetComponentCell(variableId);
                if (cell != null)
                {
                    cell.UpdateDisplay();
                }
            }
            else if (components.Length == 2)
            {
                // column / row header
                if (components[1][0] == 'c')
                {
                    string columnId = components[1].Substring(1);
                    int col = data.GetColumnIndexFromColumnId(columnId);
                    UpdateColumn(col);
                }
                else if (components[1][0] == 'r')
                {
                    string rowId = components[1].Substring(1);
                    int row = data.GetRowIndexFromRowId(rowId);
                    UpdateRow(row);
                }
            }
        }

        private void grid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        #region command bindings

        private void ColumnDelete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = grid.Columns.Count > 1;
        }

        private void RowDelete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = grid.Items.Count > 1;
        }

        private void ColumnDelete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGridColumnHeader header = UIHelper.FindVisualParent<DataGridColumnHeader>(sender as FrameworkElement);
            int col = grid.Columns.IndexOf(header.Column);
            Command.SpreadsheetDeleteColumn.Execute(col, this);
        }

        private void RowDelete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGridRowHeader header = UIHelper.FindVisualParent<DataGridRowHeader>(sender as FrameworkElement);
            DataGridRow gridRow = UIHelper.FindVisualParent<DataGridRow>(header);
            int row = (gridRow as DataGridRow).GetIndex();
            Command.SpreadsheetDeleteRow.Execute(row, this);
        }

        private void ColumnInsert_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGridColumnHeader header = UIHelper.FindVisualParent<DataGridColumnHeader>(sender as FrameworkElement);
            int col = grid.Columns.IndexOf(header.Column);
            Command.SpreadsheetAddColumn.Execute(col, this);
        }

        private void RowInsert_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGridRowHeader header = UIHelper.FindVisualParent<DataGridRowHeader>(sender as FrameworkElement);
            DataGridRow gridRow = UIHelper.FindVisualParent<DataGridRow>(header);
            int row = (gridRow as DataGridRow).GetIndex();
            Command.SpreadsheetAddRow.Execute(row, this);
        }
        
        private void ColumnText_Loaded(object sender, RoutedEventArgs e)
        {
            SpreadsheetHeaderText textBox = sender as SpreadsheetHeaderText;
            DataGridColumnHeader header = UIHelper.FindVisualParent<DataGridColumnHeader>(sender as FrameworkElement);
            int col = grid.Columns.IndexOf(header.Column);
            textBox.data = data.columnDatas[col];
        }

        private void RowText_Loaded(object sender, RoutedEventArgs e)
        {
            SpreadsheetHeaderText textBox = sender as SpreadsheetHeaderText;
            DataGridRowHeader header = UIHelper.FindVisualParent<DataGridRowHeader>(sender as FrameworkElement);
            DataGridRow gridRow = UIHelper.FindVisualParent<DataGridRow>(header);
            int row = (gridRow as DataGridRow).GetIndex();
            textBox.data = data.rowDatas[row];
        }

        private void ColumnText_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            DataGridColumnHeader header = UIHelper.FindVisualParent<DataGridColumnHeader>(sender as FrameworkElement);
            int col = grid.Columns.IndexOf(header.Column);
            string autoName = GetColumnName(col);
            // if textbox is same as automated name, remove the label instead
            if (textBox.Text != autoName)
            {
                data.RenameColumn(col, textBox.Text);
            }
            else
            {
                data.RenameColumn(col, null);
            }
            UpdateColumn(col);
            textBox.IsReadOnly = true;
        }

        private void RowText_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            DataGridRowHeader header = UIHelper.FindVisualParent<DataGridRowHeader>(sender as FrameworkElement);
            DataGridRow gridRow = UIHelper.FindVisualParent<DataGridRow>(header);
            int row = (gridRow as DataGridRow).GetIndex();
            string autoName = GetRowName(row);
            // if textbox is same as automated name, remove the label instead
            if (textBox.Text != autoName)
            {
                data.RenameRow(row, textBox.Text);
            }
            else
            {
                data.RenameRow(row, null);
            }
            UpdateRow(row);
            textBox.IsReadOnly = true;
        }

        private void ColumnRename_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TextBox textBox = UIHelper.FindVisualChild<TextBox>(sender as FrameworkElement);            
            textBox.IsReadOnly = false;
            Dispatcher.BeginInvoke(DispatcherPriority.Input,
             new Action(delegate()
             {
                 textBox.Focus();
                 Keyboard.Focus(textBox);
             }));
        }

        private void RowRename_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TextBox textBox = UIHelper.FindVisualChild<TextBox>(sender as FrameworkElement);
            textBox.IsReadOnly = false;
            Dispatcher.BeginInvoke(DispatcherPriority.Input,
             new Action(delegate()
             {
                 textBox.Focus();
                 Keyboard.Focus(textBox);
             }));
        }

        private void ColumnHeader_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Command.Rename.Execute(null, sender as IInputElement);
        }

        private void RowHeader_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Command.Rename.Execute(null, sender as IInputElement);
        }
        private void ColumnHeader_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                SpreadsheetHeaderText textBox = UIHelper.FindVisualChild<SpreadsheetHeaderText>(sender as FrameworkElement);
                textBox.DispatchClickComponent();
                if (MainWindow.ClickComponentReceiveVariable.HasValue && MainWindow.ClickComponentReceiveVariable.Value)
                {
                    e.Handled = true;
                }
            }
        }
        private void RowHeader_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                SpreadsheetHeaderText textBox = UIHelper.FindVisualChild<SpreadsheetHeaderText>(sender as FrameworkElement);
                textBox.DispatchClickComponent();
                if (MainWindow.ClickComponentReceiveVariable.HasValue && MainWindow.ClickComponentReceiveVariable.Value)
                {
                    e.Handled = true;
                }
            }
        }

        private void ColumnExpression_Loaded(object sender, RoutedEventArgs e)
        {
            SpreadsheetHeaderExpressionText expressionTextBox = sender as SpreadsheetHeaderExpressionText;
            DataGridColumnHeader header = UIHelper.FindVisualParent<DataGridColumnHeader>(sender as FrameworkElement);
            int col = grid.Columns.IndexOf(header.Column);
            expressionTextBox.valueStore = valueStore;
            expressionTextBox.data = data.columnDatas[col];
        }

        private void RowExpression_Loaded(object sender, RoutedEventArgs e)
        {
            SpreadsheetHeaderExpressionText expressionTextBox = sender as SpreadsheetHeaderExpressionText;
            DataGridRowHeader header = UIHelper.FindVisualParent<DataGridRowHeader>(sender as FrameworkElement);
            DataGridRow gridRow = UIHelper.FindVisualParent<DataGridRow>(header);
            int row = (gridRow as DataGridRow).GetIndex();
            expressionTextBox.valueStore = valueStore;
            expressionTextBox.data = data.rowDatas[row];
        }


        private void ColumnExpression_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGridColumnHeader header = UIHelper.FindVisualParent<DataGridColumnHeader>(sender as FrameworkElement);
            SpreadsheetHeaderExpressionText expressionTextBox = UIHelper.FindVisualChild<SpreadsheetHeaderExpressionText>(header);


            Dispatcher.BeginInvoke(DispatcherPriority.Input,
             new Action(delegate()
             {
                 expressionTextBox.GetFocusBack();
             }));
        }
        private void ColumnExpression_GotFocus(object sender, RoutedEventArgs e)
        {
            SpreadsheetHeaderExpressionText expressionTextBox = sender as SpreadsheetHeaderExpressionText;
            DataGridColumnHeader header = UIHelper.FindVisualParent<DataGridColumnHeader>(sender as FrameworkElement);
            TextBox textBox = UIHelper.FindVisualChild<TextBox>(header);
            // TODO: there should be better way to hide the text than cleaning the header, 
            // it's a workaround to fix the blank glitch if the expressionText contains variable
            //textBox.Visibility = Visibility.Hidden;
            int col = grid.Columns.IndexOf(header.Column);
            grid.Columns[col].Header = "";

        }
        private void ColumnExpression_LostFocus(object sender, RoutedEventArgs e)
        {
            SpreadsheetHeaderExpressionText expressionTextBox = sender as SpreadsheetHeaderExpressionText;
            DataGridColumnHeader header = UIHelper.FindVisualParent<DataGridColumnHeader>(sender as FrameworkElement);
            TextBox textBox = UIHelper.FindVisualChild<TextBox>(header);
            int col = grid.Columns.IndexOf(header.Column);

            string expression = expressionTextBox.GetExpression();
            data.SetColumnExpression(col, expression);

            expressionTextBox.UpdateDisplay();
            expressionTextBox.Visibility = Visibility.Hidden;
            //textBox.Visibility = Visibility.Visible;
            UpdateColumn(col);
        }

        private void RowExpression_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGridRowHeader header = UIHelper.FindVisualParent<DataGridRowHeader>(sender as FrameworkElement);
            SpreadsheetHeaderExpressionText expressionTextBox = UIHelper.FindVisualChild<SpreadsheetHeaderExpressionText>(header);

            Dispatcher.BeginInvoke(DispatcherPriority.Input,
             new Action(delegate()
             {
                 expressionTextBox.GetFocusBack();
             }));
        }
        private void RowExpression_GotFocus(object sender, RoutedEventArgs e)
        {
            SpreadsheetHeaderExpressionText expressionTextBox = sender as SpreadsheetHeaderExpressionText;
            DataGridRowHeader header = UIHelper.FindVisualParent<DataGridRowHeader>(sender as FrameworkElement);
            DataGridRow gridRow = UIHelper.FindVisualParent<DataGridRow>(header);
            TextBox textBox = UIHelper.FindVisualChild<TextBox>(header);
            int row = (gridRow as DataGridRow).GetIndex();

            // TODO: there should be better way to hide the text than cleaning the header, 
            // it's a workaround to fix the blank glitch if the expressionText contains variable
            //textBox.Visibility = Visibility.Hidden;
            gridRow.Header = "";

        }
        private void RowExpression_LostFocus(object sender, RoutedEventArgs e)
        {
            SpreadsheetHeaderExpressionText expressionTextBox = sender as SpreadsheetHeaderExpressionText;
            DataGridRowHeader header = UIHelper.FindVisualParent<DataGridRowHeader>(sender as FrameworkElement);
            DataGridRow gridRow = UIHelper.FindVisualParent<DataGridRow>(header);
            TextBox textBox = UIHelper.FindVisualChild<TextBox>(header);
            int row = (gridRow as DataGridRow).GetIndex();

            string expression = expressionTextBox.GetExpression();
            data.SetRowExpression(row, expression);

            expressionTextBox.UpdateDisplay();
            expressionTextBox.Visibility = Visibility.Hidden;
            //textBox.Visibility = Visibility.Visible;
            UpdateRow(row);
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                /*DependencyObject scope = FocusManager.GetFocusScope(this);
                FocusManager.SetFocusedElement(scope, Application.Current.MainWindow);
                Keyboard.Focus(Application.Current.MainWindow);*/
                RoutedUICommand command = Command.UnselectComponent;
                command.Execute(sender, Application.Current.MainWindow);
            }
        }

        #endregion

        private void UpdateColumn(int col)
        {
            SpreadsheetColumn column = grid.Columns[col] as SpreadsheetColumn;
            column.Binding = new Binding("[" + col + "]");
            if (data.columnDatas[col].HasCustomLabel())
            {
                column.Header = data.columnDatas[col].label;                
            }
            else
            {
                column.Header = "";
                column.Header = GetColumnName(col);
            }
            column.IsReadOnly = data.columnDatas[col].HasExpression();
        }

        public void DeleteColumn(int? col)
        {
            int colIndex = (col.HasValue ? col.Value : (grid.Columns.Count - 1));
            grid.Columns.RemoveAt(colIndex);
            // update the columns binding and header
            for (int i = colIndex; i < grid.Columns.Count; ++i)
            {
                UpdateColumn(i);
            }
        }

        public void DeleteRow(int? row)
        {
            //grid.Items.Refresh();
            for (int i = 0; i < grid.Items.Count; ++i)
            {
                UpdateRow(i);
            }
        }

        private void Button_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            (sender as Button).Focus();
            e.Handled = true;
        }

        /*private void grid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (grid.SelectedCells.Count == 1)
            {
                DataGridCellInfo cellInfo = grid.SelectedCells[0];
                SpreadsheetComponentCell cell = cellInfo.Column.GetCellContent(cellInfo.Item) as SpreadsheetComponentCell;
                Command.ClickComponent.Execute(cell.textBox, Application.Current.MainWindow);
            }
        }*/

        private void grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = e.OriginalSource as DependencyObject;

            // check if one/more cell is selected
            DataGridCell cell = null;
            try
            {
                cell = UIHelper.FindVisualParent<DataGridCell>(dep);

            }
            catch (Exception exc)
            {
            }

            if (cell != null)
            {
                if (grid.SelectedCells.Count > 0)
                {
                    Command.ClickComponent.Execute(this, Application.Current.MainWindow);
                }
            }
        }

        public AbstractComponentData GetData()
        {
            if (grid.SelectedCells.Count == 0){
                return null;
            }
            else if (grid.SelectedCells.Count == 1)
            {
                DataGridCellInfo cellInfo = grid.SelectedCells[0];
                SpreadsheetComponentCell cell = cellInfo.Column.GetCellContent(cellInfo.Item) as SpreadsheetComponentCell;
                return cell.data;
            }
            else
            {
                PointInt rangeStart = new PointInt(grid.Columns.Count, grid.Items.Count);
                PointInt rangeEnd = new PointInt(-1,-1);
                foreach (DataGridCellInfo cellInfo in grid.SelectedCells){
                    PointInt position = GetCellPositionFromCellInfo(cellInfo);
                    rangeStart.X = Math.Min(rangeStart.X, position.X);
                    rangeStart.Y = Math.Min(rangeStart.Y, position.Y);
                    rangeEnd.X = Math.Max(rangeEnd.X, position.X);
                    rangeEnd.Y = Math.Max(rangeEnd.Y, position.Y);
                }
                string startId = data.cells[rangeStart.Y][rangeStart.X].id;
                string endId = data.cells[rangeEnd.Y][rangeEnd.X].id;
                SpreadsheetRangeData rangeData = new SpreadsheetRangeData(this.valueStore, this.data, startId, endId);
                return rangeData;
            } 
        }

        private PointInt GetCellPositionFromCellInfo(DataGridCellInfo cellInfo)
        {
            int rowIndex = grid.Items.IndexOf(cellInfo.Item);
            int columnIndex = grid.Columns.IndexOf(cellInfo.Column);
            return new PointInt(columnIndex, rowIndex);
        }

        private void gridDelete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (grid.SelectedCells.Count == 1)
            {
                DataGridCellInfo cellInfo = grid.SelectedCells[0];
                SpreadsheetComponentCell cell = cellInfo.Column.GetCellContent(cellInfo.Item) as SpreadsheetComponentCell;
                if (cell.data.dataTableData != null)
                {
                    MessageBoxResult result = MessageBox.Show("Do you want to delete the entire Data Table?", "Delete Data Table", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        Command.DeleteDatatable.Execute(cell.data.dataTableData, Application.Current.MainWindow);
                    }
                }
            }
        }

    }

    class SpreadsheetHeaderText: TextBox, IComponentVariableSource
    {
        public AbstractComponentData data;

        public ControlConnector connector { get; private set; }

        public SpreadsheetHeaderText()
            : base()
        {
            connector = new ControlConnector();
            connector.HorizontalAlignment = HorizontalAlignment.Center;
            connector.VerticalAlignment = VerticalAlignment.Center;
            this.AddVisualChild(connector);
            connector.InitConnectable(this);

            this.Unloaded += SpreadsheetHeaderText_Unloaded;
        }

        void SpreadsheetHeaderText_Unloaded(object sender, RoutedEventArgs e)
        {
            connector.Cleanup();
        }

        public AbstractComponentData GetData()
        {
            return data;
        }
        
        /*protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (DispatchClickComponent())
                {
                    e.Handled = true;
                }
            }
        }*/

        public bool DispatchClickComponent()
        {
            if (!this.IsFocused)
            {
                RoutedUICommand command = Command.ClickComponent;
                command.Execute(this, Application.Current.MainWindow);
                return true;
            }
            return false;
        }

    }

    class SpreadsheetHeaderExpressionText : ExpressionTextBox, IComponentVariableReceiver
    {
        public AbstractComponentData data;

        public SpreadsheetHeaderExpressionText()
            : base()
        {
            this.GotFocus += SpreadsheetHeaderExpressionText_GotFocus;
            this.Background = Brushes.White;
        }

        void SpreadsheetHeaderExpressionText_GotFocus(object sender, RoutedEventArgs e)
        {
            RoutedUICommand command = Command.ActivateComponent;
            command.Execute(sender, Application.Current.MainWindow);
            UpdateDisplay();
        }

        public override void GetFocusBack()
        {
            this.Visibility = Visibility.Visible;
            base.GetFocusBack();
        }

        public void UpdateDisplay()
        {
            if (this.IsFocused)
            {
                this.SetExpression((data as IExpressionData).expression);
            }
            else
            {
                this.SetPlainText("");
            }
        }

        public bool ReceiveComponentVariable(IComponentVariableSource component)
        {
            return this.ReceiveComponentVariable(component, data);
        }
    }
}
