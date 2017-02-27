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
using System.Text.RegularExpressions;
using DiagramDesigner;

namespace InfluenceDiagram.ComponentControl
{
    /// <summary>
    /// Interaction logic for SpreadsheetComponentCell.xaml
    /// </summary>
    public partial class SpreadsheetComponentCell : UserControl
    {
        IComponentValueStore valueStore
        {
            get { return data.valueStore; }
        }

        #region Data property

        public SpreadsheetCellData data
        {            
            get { return (SpreadsheetCellData)GetValue(DataProperty); }
            set
            {
                SetValue(DataProperty, value);
            }
        }
        public static readonly DependencyProperty DataProperty =
          DependencyProperty.Register("Data",
                                       typeof(SpreadsheetCellData),
                                       typeof(SpreadsheetComponentCell),
                                       new FrameworkPropertyMetadata(new PropertyChangedCallback(OnDataChanged))
                                       );
        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SpreadsheetComponentCell)d).OnDataChanged((SpreadsheetCellData)e.NewValue, (SpreadsheetCellData)e.OldValue);
        }
        private void OnDataChanged(SpreadsheetCellData value, SpreadsheetCellData oldValue)
        {
            this.textBox.valueStore = value.valueStore;
            this.textBox.data = value;
            valueStore.TriggerEdgeForComponent(data.id);
            UpdateDisplay();
        }

        public bool IsColumnReadOnly
        {
            get { return (bool)GetValue(IsColumnReadOnlyProperty); }
            set { SetValue(IsColumnReadOnlyProperty, value); }
        }
        public static readonly DependencyProperty IsColumnReadOnlyProperty =
          DependencyProperty.Register("IsColumnReadOnly",
                                       typeof(bool),
                                       typeof(SpreadsheetComponentCell),
                                       new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsColumnReadOnlyChanged))
                                       );
        private static void OnIsColumnReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpreadsheetComponentCell obj = ((SpreadsheetComponentCell)d);
            obj.IsReadOnly = (bool)e.NewValue || obj.IsRowReadOnly;
        }

        public bool IsRowReadOnly
        {
            get { return (bool)GetValue(IsRowReadOnlyProperty); }
            set { SetValue(IsRowReadOnlyProperty, value); }
        }
        public static readonly DependencyProperty IsRowReadOnlyProperty =
            DependencyProperty.Register("IsRowReadOnly",
                                       typeof(bool),
                                       typeof(SpreadsheetComponentCell),
                                       new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsRowReadOnlyChanged))
                                       );
        private static void OnIsRowReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpreadsheetComponentCell obj = ((SpreadsheetComponentCell)d);
            obj.IsReadOnly = (bool)e.NewValue || obj.IsColumnReadOnly;
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { 
                SetValue(IsReadOnlyProperty, value); 
            }
        }
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly",
                                       typeof(bool),
                                       typeof(SpreadsheetComponentCell),
                                       new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsReadOnlyChanged))
                                       );
        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpreadsheetComponentCell obj = ((SpreadsheetComponentCell)d);
            obj.IsFocusable = !(bool)e.NewValue && obj.IsEditing;   // IsFocusable = !IsReadOnly && IsEditing
        }

        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }
        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register("IsEditing",
                                       typeof(bool),
                                       typeof(SpreadsheetComponentCell),
                                       new FrameworkPropertyMetadata(new PropertyChangedCallback(OnIsEditingChanged))
                                       );
        private static void OnIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpreadsheetComponentCell obj = ((SpreadsheetComponentCell)d);
            obj.IsFocusable = !obj.IsReadOnly && (bool)e.NewValue;   // IsFocusable = !IsReadOnly && IsEditing
        }

        public bool IsFocusable
        {
            get { return (bool)GetValue(IsFocusableProperty); }
            set { SetValue(IsFocusableProperty, value); }
        }
        public static readonly DependencyProperty IsFocusableProperty =
            DependencyProperty.Register("IsFocusable",
                                       typeof(bool),
                                       typeof(SpreadsheetComponentCell)
                                       );

        #endregion

        bool hasInitBinding = false;

        public SpreadsheetComponentCell()
        {
            InitializeComponent();

            this.Loaded += SpreadsheetComponentCell_Loaded;
        }

        void SpreadsheetComponentCell_Loaded(object sender, RoutedEventArgs e)
        {
            InitBinding();
        }

        void InitBinding()
        {
            if (!hasInitBinding)
            {
                DataGridCell cell = this.Parent as DataGridCell;
                DataGridRow gridRow = UIHelper.FindVisualParent<DataGridRow>(cell);

                this.IsRowReadOnly = gridRow.Tag is Boolean ? (bool)gridRow.Tag : false;
                Binding b = new Binding("Tag") { Source = gridRow, Mode = BindingMode.OneWay };
                this.SetBinding(SpreadsheetComponentCell.IsRowReadOnlyProperty, b);

                this.hasInitBinding = true;
            }
        }

        public void UpdateDisplay()
        {
            if (!this.IsReadOnly && textBox.IsFocused)
            {
                // when focused, show the expression
                textBox.SetExpression(data.expression);
            }
            else
            {
                // when not focused, show the evaluated value
                textBox.SetPlainText(data.GetValueAsString());
            }
        }

        private void UpdateData()
        {
            if (this.data.dataTableData != null && !this.data.dataTableData.IsHeader)
            {
                // cannot edit a cell containing dataTable content, do nothing
            }
            else
            {
                string newExpression = textBox.GetExpression();
                //if (newExpression != this.data.expression)
                //{
                this.data.expression = newExpression;
                //}
            }
        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            InitBinding();
            if (!this.IsReadOnly)
            {
                RoutedUICommand command = Command.ActivateComponent;
                command.Execute(sender, Application.Current.MainWindow);
            }
            UpdateDisplay();
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateData();
            RoutedUICommand command = Command.DeactivateComponent;
            command.Execute(sender, Application.Current.MainWindow);
            UpdateDisplay();
        }
    }

    class SpreadsheetCellText : ExpressionTextBox, IComponentVariableReceiver, IComponentVariableSource
    {
        public SpreadsheetCellData data;

        public SpreadsheetCellText()
            : base()
        {
            this.Background = null;
        }

        public bool ReceiveComponentData(AbstractComponentData receivedData)
        {
            return this.ReceiveComponentData(receivedData, data);
        }

        public bool ReceiveComponentVariable(IComponentVariableSource component)
        {
            return this.ReceiveComponentVariable(component, data);
        }

        public AbstractComponentData GetData()
        {
            return data;
        }

        protected override void OnPressReturn(object sender, KeyEventArgs e)
        {
            if (this.data.dataTableData != null && !this.data.dataTableData.IsHeader)
            {
                // cannot edit a cell containing dataTable content, show error
                MessageBox.Show("Cannot change part of a data table");
            }
            base.OnPressReturn(sender, e);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (!this.IsFocused)
            {
                RoutedUICommand command = Command.ClickComponent;
                command.Execute(this, Application.Current.MainWindow);
            }
        }
    }
}
