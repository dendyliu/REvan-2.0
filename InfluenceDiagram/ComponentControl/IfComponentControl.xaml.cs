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

namespace InfluenceDiagram.ComponentControl
{
    /// <summary>
    /// Interaction logic for IfComponentControl.xaml
    /// </summary>
    public partial class IfComponentControl : UserControl, IComponentControl, IConnectable, IComponentVariableSource
    {
        public IfComponentData data {get; private set;}
        public RootComponentData rootData { get { return data; } }
        private IComponentValueStore valueStore;

        public IConnectable GetConnector(string variableId)
        {
            if (variableId == data.id)
            {
                return this;
            }
            else if (variableId == data.conditionData.id)
            {
                return textCondition;
            }
            else if (variableId == data.trueData.id)
            {
                return textTrue;
            }
            else if (variableId == data.falseData.id)
            {
                return textFalse;
            }
            return null;
        }

        public ControlConnector connector { get; private set; }

        public IfComponentControl(IfComponentData data, IComponentValueStore valueStore)
        {
            InitializeComponent();
            this.Loaded += IfComponentControl_Loaded;
            this.data = data;
            this.valueStore = valueStore;

            textCondition.data = data.conditionData;
            textTrue.data = data.trueData;
            textFalse.data = data.falseData;
            textCondition.valueStore = valueStore;
            textTrue.valueStore = valueStore;
            textFalse.valueStore = valueStore;
            textCondition.placeholder = "if";
            textTrue.placeholder = "then";
            textFalse.placeholder = "else";

            connector = new ControlConnector();
            connector.HorizontalAlignment = HorizontalAlignment.Center;
            connector.VerticalAlignment = VerticalAlignment.Center;
            this.AddVisualChild(connector);
            connector.InitConnectable(this);

            this.Unloaded += IfComponentControl_Unloaded;
        }

        void IfComponentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            connector.Cleanup();
            data.PropertyChanged -= data_PropertyChanged;
        }
        
        void IfComponentControl_Loaded(object sender, RoutedEventArgs e)
        {
            DependencyObject parent = this.Parent as DependencyObject;
            data.BindPositionToCanvas(parent);
            data.PropertyChanged += data_PropertyChanged;
            UpdateDisplay();
        }

        void data_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "label")
            {
                UpdateDisplay();
            }
        }

        public void UpdateDisplay()
        {
            textCondition.UpdateDisplay();
            textTrue.UpdateDisplay();
            textFalse.UpdateDisplay();
            textLabel.Text = data.label;
        }

        public AbstractComponentData GetData()
        {
            return data;
        }

        private void textLabel_LostFocus(object sender, RoutedEventArgs e)
        {
            data.label = textLabel.Text;
        }

        private void textBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            IfComponentText textBox = sender as IfComponentText;
            Grid grid = UIHelper.FindVisualParent<Grid>(textBox);
            Polygon border = UIHelper.FindVisualChild<Polygon>(grid);
            BorderDecorator.DecorateBorderHexagon(border, textBox);
        }
    }

    public class IfComponentText: ExpressionTextBox, IComponentVariableReceiver, IComponentVariableSource, IConnectable
    {
        public IfExpressionData data;
        public string placeholder;
        
        public IfComponentText()
            : base()
        {
            this.GotFocus += IfComponentText_GotFocus;
            this.LostFocus += IfComponentText_LostFocus;
        }

        void IfComponentText_GotFocus(object sender, RoutedEventArgs e)
        {
            RoutedUICommand command = Command.ActivateComponent;
            command.Execute(sender, Application.Current.MainWindow);
            UpdateDisplay();
        }

        void IfComponentText_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateExpressionData();
            RoutedUICommand command = Command.DeactivateComponent;
            command.Execute(sender, Application.Current.MainWindow);
            UpdateDisplay();        
        }

        public void UpdateDisplay()
        {
            bool isPlaceholder = false;
            if (this.IsFocused)
            {
                this.SetExpression(data.expression);
            }
            else
            {
                if (data.expression.Length == 0)
                {
                    this.SetPlainText(placeholder);
                    isPlaceholder = true;
                }
                else
                {
                    this.SetPlainText(data.GetValueAsString());
                }
            }
            this.FontWeight = isPlaceholder ? FontWeights.Normal : FontWeights.Bold;
            this.FontStyle = isPlaceholder ? FontStyles.Italic : FontStyles.Normal;
        }

        private void UpdateExpressionData()
        {
            string newExpression = this.GetExpression();
            this.data.expression = newExpression;
        }

        public bool ReceiveComponentVariable(IComponentVariableSource component)
        {
            string parentId = valueStore.GetRootComponentId(data.id);
            string otherParentId = valueStore.GetRootComponentId(component.GetData().id);
            if (parentId == otherParentId) return false;
            return this.ReceiveComponentVariable(component, data);
        }

        // as variable source, return the IfComponentData, not the expression
        public AbstractComponentData GetData()
        {
            return UIHelper.FindVisualParent<IfComponentControl>(this).data;
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
