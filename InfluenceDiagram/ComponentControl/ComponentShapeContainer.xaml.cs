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

namespace InfluenceDiagram.ComponentControl
{

    enum ShapeCategory {
        Border,
        Polygon
    }

    /// <summary>
    /// Interaction logic for ComponentShapeContainer.xaml
    /// </summary>
    public partial class ComponentShapeContainer : UserControl
    {

        #region dependency properties

        public ComponentShape Shape
        {
            get { return (ComponentShape)GetValue(ComponentShapeProperty); }
            set { SetValue(ComponentShapeProperty, value); }
        }
        public static readonly DependencyProperty ComponentShapeProperty =
            DependencyProperty.Register("Shape",
                                       typeof(ComponentShape),
                                       typeof(ComponentShapeContainer),
                                       new FrameworkPropertyMetadata(new PropertyChangedCallback(OnComponentShapeChanged))
                                       );
        private static void OnComponentShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ComponentShapeContainer)d).OnComponentShapeChanged();
        }

        public Color BorderColor
        {
            get { return (Color)GetValue(BorderColorProperty); }
            set { SetValue(BorderColorProperty, value); }
        }
        public static readonly DependencyProperty BorderColorProperty =
            DependencyProperty.Register("BorderColor",
                                       typeof(Color),
                                       typeof(ComponentShapeContainer)
                                       );

        public Color BackgroundColor
        {
            get { return (Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor",
                                       typeof(Color),
                                       typeof(ComponentShapeContainer)
                                       );

        public FrameworkElement ContainedElement
        {
            get { return (FrameworkElement)GetValue(ContainedElementProperty); }
            set { SetValue(ContainedElementProperty, value); }
        }
        public static readonly DependencyProperty ContainedElementProperty =
            DependencyProperty.Register("ContainedElement",
                                       typeof(FrameworkElement),
                                       typeof(ComponentShapeContainer)
                                       );

        #endregion



        object shapeElement;

        public ComponentShapeContainer()
        {
            InitializeComponent();
            OnComponentShapeChanged();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            DecoratePolygon();
        }

        ShapeCategory CheckShapeCategory(ComponentShape shape)
        {
            switch (shape) {
                case ComponentShape.Hexagon:
                case ComponentShape.Octagon:
                    return ShapeCategory.Polygon;
                default:
                    return ShapeCategory.Border;
            }
        } 

        void OnComponentShapeChanged()
        {
            ComponentShape shape = this.Shape;
            container.Children.Clear();

            ShapeCategory shapeCategory = CheckShapeCategory(shape);
            if (shapeCategory == ShapeCategory.Border)
            {
                Border border = new Border();
                shapeElement = border;
                container.Children.Add(border);

                SolidColorBrush brush = new SolidColorBrush();
                border.BorderBrush = brush;
                Binding binding = new Binding("BorderColor") { Source = this, Mode = BindingMode.OneWay };
                BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, binding);

                brush = new SolidColorBrush();
                border.Background = brush;
                binding = new Binding("BackgroundColor") { Source = this, Mode = BindingMode.OneWay };
                BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, binding);

                binding = new Binding("BorderThickness") { Source = this, Mode = BindingMode.OneWay };
                border.SetBinding(Border.BorderThicknessProperty, binding);

                if (shape == ComponentShape.RectangleRounded1)
                {
                    border.CornerRadius = new CornerRadius(10);
                }
                else if (shape == ComponentShape.RectangleRounded2)
                {
                    border.CornerRadius = new CornerRadius(5);
                }
            }
            else if (shapeCategory == ShapeCategory.Polygon)
            {
                Polygon polygon = new Polygon();
                shapeElement = polygon;
                container.Children.Add(polygon);

                SolidColorBrush brush = new SolidColorBrush();
                polygon.Stroke = brush;
                Binding binding = new Binding("BorderColor") { Source = this, Mode = BindingMode.OneWay };
                BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, binding);

                brush = new SolidColorBrush();
                polygon.Fill = brush;
                binding = new Binding("BackgroundColor") { Source = this, Mode = BindingMode.OneWay };
                BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, binding);

                binding = new Binding("BorderThickness") { Source = this, Mode = BindingMode.OneWay };
                polygon.SetBinding(Polygon.StrokeThicknessProperty, binding);

                DecoratePolygon();
            }
        }

        void DecoratePolygon()
        {
            if (shapeElement is Polygon && ContainedElement != null)
            {
                Polygon polygon = shapeElement as Polygon;
                if (this.Shape == ComponentShape.Hexagon)
                {
                    BorderDecorator.DecorateBorderHexagon(polygon, ContainedElement);
                }
                else if (this.Shape == ComponentShape.Octagon)
                {
                    BorderDecorator.DecorateBorderOctagon(polygon, ContainedElement);
                }
            }
        }

    }
}
