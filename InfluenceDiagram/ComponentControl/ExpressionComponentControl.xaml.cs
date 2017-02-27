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
using DiagramDesigner;
using InfluenceDiagram.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using InfluenceDiagram.Utility;

namespace InfluenceDiagram.ComponentControl
{
    /// <summary>
    /// Interaction logic for ExpressionComponentControl.xaml
    /// </summary>
    public partial class ExpressionComponentControl : UserControl, IComponentControl
    {
        public ExpressionComponentData data { get; private set; }
        public RootComponentData rootData { get { return data; } }
        private IComponentValueStore valueStore;

        public IConnectable GetConnector(string variableId)
        {
            if (variableId == data.id)
            {
                return textBox;
            }
            return null;
        }
        
        public ExpressionComponentControl(ExpressionComponentData data, IComponentValueStore valueStore)
        {
            this.data = data;
            this.valueStore = valueStore;

            InitializeComponent();
            this.Loaded += ExpressionComponentControl_Loaded;
            this.Unloaded += ExpressionComponentControl_Unloaded;

            textBox.Document.TextAlignment = TextAlignment.Center;
            textBox.data = data;
            textBox.valueStore = valueStore;            
        }

        void ExpressionComponentControl_Loaded(object sender, RoutedEventArgs e)
        {
            DependencyObject parent = this.Parent as DependencyObject;
            data.BindPositionToCanvas(parent);
            data.PropertyChanged += data_PropertyChanged;
            UpdateDisplay();
        }

        void ExpressionComponentControl_Unloaded(object sender, RoutedEventArgs e)
        {
            data.PropertyChanged -= data_PropertyChanged;
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
        
        public void UpdateDisplay()
        {
            textBox.UpdateDisplay();
            textLabel.Text = data.label;
        }
        
        private void UpdateData()
        {
            string newExpression = textBox.GetExpression();
            //if (newExpression != this.data.expression)
            //{
                this.data.expression = newExpression;
            //}
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            // keep element centered
            /*double deltaWidth = sizeInfo.PreviousSize.Width - sizeInfo.NewSize.Width;
            UIElement parent = this.Parent as UIElement;
            Canvas.SetLeft(parent, Canvas.GetLeft(parent) + deltaWidth / 2);            */
        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            RoutedUICommand command = Command.ActivateComponent;
            command.Execute(sender, Application.Current.MainWindow);
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

    class ExpressionComponentText: ExpressionTextBox, IComponentVariableReceiver, IComponentVariableSource
    {
        public ExpressionComponentData data;

        public ExpressionComponentText()
            : base()
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
            return this.ReceiveComponentVariable(component, data);
        }

        public AbstractComponentData GetData()
        {
            return data;
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
