using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace InfluenceDiagram.ComponentControl
{
    class BorderDecorator
    {
        static public void DecorateBorderHexagon(Polygon border, FrameworkElement element)
        {
            border.Points.Clear();
            double stroke = border.StrokeThickness;
            double halfHeight = element.ActualHeight / 2;
            element.Margin = new Thickness(halfHeight, stroke, halfHeight + stroke, 2 * stroke);
            border.Points.Add(new Point(halfHeight, 0));
            border.Points.Add(new Point(halfHeight + element.ActualWidth, 0));
            border.Points.Add(new Point(halfHeight * 2 + element.ActualWidth, halfHeight + stroke));
            border.Points.Add(new Point(halfHeight + element.ActualWidth, element.ActualHeight + 2 * stroke));
            border.Points.Add(new Point(halfHeight, element.ActualHeight + 2 * stroke));
            border.Points.Add(new Point(0, halfHeight + stroke));

        }

        static public void DecorateBorderOctagon(Polygon border, FrameworkElement element)
        {
            border.Points.Clear();
            element.Margin = new Thickness(5, 5, 5, 5);
            border.Points.Add(new Point(5, 0));
            border.Points.Add(new Point(5 + element.ActualWidth - border.StrokeThickness, 0));
            border.Points.Add(new Point(10 + element.ActualWidth - border.StrokeThickness, 5));
            border.Points.Add(new Point(10 + element.ActualWidth - border.StrokeThickness, element.ActualHeight + 5));
            border.Points.Add(new Point(5 + element.ActualWidth - border.StrokeThickness, element.ActualHeight + 10));
            border.Points.Add(new Point(5, element.ActualHeight + 10));
            border.Points.Add(new Point(0, element.ActualHeight + 5));
            border.Points.Add(new Point(0, 5));
        }
    }
}
