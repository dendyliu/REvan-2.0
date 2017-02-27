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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace InfluenceDiagram.ComponentControl
{
    /// <summary>
    /// Interaction logic for ComponentShapeEditor.xaml
    /// </summary>
    public partial class ComponentShapeEditor : UserControl, ITypeEditor
    {

        public static readonly DependencyProperty ShapeProperty =
            DependencyProperty.Register("Shape", 
                typeof(ComponentShape), 
                typeof(ComponentShapeEditor)
            );

        public ComponentShape Shape
        {
            get { return (ComponentShape)GetValue(ShapeProperty); }
            set { SetValue(ShapeProperty, value); }
        }

        public ComponentShapeEditor()
        {
            InitializeComponent();
        }

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            Binding binding = new Binding("Value");
            binding.Source = propertyItem;
            binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(this, ComponentShapeEditor.ShapeProperty, binding);

            binding = new Binding("Shape");
            binding.Source = this;
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(box, ComboBox.SelectedValueProperty, binding);

            return this;
        }
    }
}
