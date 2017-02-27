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

namespace InfluenceDiagram.ComponentControl
{
    /// <summary>
    /// Interaction logic for ShapreComponentControl.xaml
    /// </summary>
    public partial class ShapeComponentControl : UserControl, IComponentControl
    {
        public ShapeComponentData data { get; private set; }
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

        public ShapeComponentControl(ShapeComponentData data, IComponentValueStore valueStore)
        {
            this.data = data;
            this.valueStore = valueStore;

            InitializeComponent();
            this.Loaded += ShapeComponentControl_Loaded;
            this.Unloaded += ShapeComponentControl_Unloaded;
            textBox.data = data;
            textBox.valueStore = valueStore;
        }

        void ShapeComponentControl_Loaded(object sender, RoutedEventArgs e)
        {
            DependencyObject parent = this.Parent as DependencyObject;
            data.BindPositionToCanvas(parent);
            data.PropertyChanged += data_PropertyChanged;
            UpdateDisplay();
        }

        void ShapeComponentControl_Unloaded(object sender, RoutedEventArgs e)
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

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            // keep element centered
            /*double deltaWidth = sizeInfo.PreviousSize.Width - sizeInfo.NewSize.Width;
            UIElement parent = this.Parent as UIElement;
            Canvas.SetLeft(parent, Canvas.GetLeft(parent) + deltaWidth / 2);*/

            //BorderDecorator.DecorateBorderOctagon(border, textBox);
        }

        private void UpdateData()
        {
            this.data.label = textBox.Text;
        }

        public void UpdateDisplay()
        {
            textBox.Text = this.data.label;
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

    public class ShapeComponentText : TextBox, IConnectable
    {
        public ShapeComponentData data;
        public ControlConnector connector { get; private set; }
        public IComponentValueStore valueStore;

        public ShapeComponentText()
            : base()
        {
            connector = new ControlConnector();
            connector.HorizontalAlignment = HorizontalAlignment.Center;
            connector.VerticalAlignment = VerticalAlignment.Center;
            connector.CanSelectConnection = true;
            this.AddVisualChild(connector);
            connector.InitConnectable(this);

            this.IsEnabled = true;
            this.MinWidth = 20;
            this.Background = null;
            this.AcceptsReturn = false;
            this.AcceptsTab = false;
            this.BorderThickness = new Thickness(0);
            this.TextAlignment = TextAlignment.Center;
            this.HorizontalAlignment = HorizontalAlignment.Center;
            this.VerticalAlignment = VerticalAlignment.Center;
            this.Padding = new Thickness(0);
            this.Margin = new Thickness(0);
            //this.FontSize = 14;
            //this.FontWeight = FontWeights.Bold;

            this.Unloaded += ShapeComponentText_Unloaded;
            this.KeyDown += ShapeComponentText_KeyDown;
        }
        
        void ShapeComponentText_KeyDown(object sender, KeyEventArgs e)
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

        void ShapeComponentText_Unloaded(object sender, RoutedEventArgs e)
        {
            connector.Cleanup();
        }

        public void CreateConnection(ShapeComponentText otherShape)
        {
            ShapeComponentData otherData = otherShape.GetData();
            Command.ShapeAddConnection.Execute(otherData, this);
        }

        public ShapeComponentData GetData()
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
