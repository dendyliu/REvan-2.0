using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DiagramDesigner;
using System.Windows.Media;
using System.Windows;
using InfluenceDiagram.Utility;
using System.Windows.Input;
using System.Windows.Controls;

namespace InfluenceDiagram.ComponentControl
{
    public class ControlConnector: DiagramDesigner.Connector
    {
        public bool CanSelectConnection;

        public ControlConnector()
            : base()
        {
            CanSelectConnection = false;
            this.IsHitTestVisible = false;
        }

        void SetConnectionOpaque(bool opaque)
        {
            foreach (ControlConnection connection in Connections)
            {
                connection.IsOpaque = opaque;
            }
        }

        public void InitConnectable(IConnectable connectable)
        {
            if (connectable is UIElement)
            {
                UIElement element = connectable as UIElement;
                element.GotFocus += element_GotFocus;
                element.LostFocus += element_LostFocus;
            }
        }

        void element_GotFocus(object sender, RoutedEventArgs e)
        {
            SetConnectionOpaque(true);
        }

        void element_LostFocus(object sender, RoutedEventArgs e)
        {
            SetConnectionOpaque(false);
        }

        public void Cleanup()
        {
            // TODO: this creates exception on the Visual Studio designer, need to fix somehow
            DesignerCanvas canvas = (Application.Current.MainWindow as MainWindow).designerCanvas;
            foreach (ControlConnection connection in this.Connections){
                canvas.Children.Remove(connection);
            }
        }

        override protected DesignerCanvas GetDesignerCanvas(DependencyObject element)
        {
            return UIHelper.FindVisualParent<DesignerCanvas>(this);
        }

        internal ControlConnectorInfo GetControlInfo()
        {
            ControlConnectorInfo info = new ControlConnectorInfo();
            info.Orientation = this.Orientation;
            info.Position = this.Position;
            return info;
        }

        // when the layout changes we update the position property
        protected override void Connector_LayoutUpdated(object sender, EventArgs e)
        {
            DesignerCanvas designer = GetDesignerCanvas(this);
            if (designer != null)
            {
                UIElement parent = UIHelper.FindVisualParent<UIElement>(this);
                this.Position = this.TranslatePoint(new Point(parent.RenderSize.Width / 2, parent.RenderSize.Height / 2), GetDesignerCanvas(this));            
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
        }
    }

    internal struct ControlConnectorInfo
    {
        //public Point CanvasPosition { get; set; }
        public Point Position { get; set; }
        public ConnectorOrientation Orientation { get; set; }
    }

    public class ControlConnection: DiagramDesigner.Connection
    {
        private Point _arrowCenterPosition;
        public Point ArrowCenterPosition
        {
            get { return _arrowCenterPosition; }
            set
            {
                if (_arrowCenterPosition != value)
                {
                    _arrowCenterPosition = value;
                    OnPropertyChanged("ArrowCenterPosition");
                }
            }
        }
        private double _arrowCenterAngle = 0;
        public double ArrowCenterAngle
        {
            get { return _arrowCenterAngle; }
            set
            {
                if (_arrowCenterAngle != value)
                {
                    _arrowCenterAngle = value;
                    OnPropertyChanged("ArrowCenterAngle");
                }
            }
        }

        public int EdgeCount { get; private set; }
        private bool _isSelectable;
        public bool IsSelectable
        {
            get { return _isSelectable; }
            set
            {
                if (_isSelectable != value)
                {
                    _isSelectable = value;
                    UpdateAnchorPosition();
                    OnPropertyChanged("IsSelectable");
                }
            }
        }
        
        // arrow is inversed except for Shape
        bool InverseArrow
        {
            get { return !this.IsSelectable; }
        }

        private bool _IsOpaque;
        public bool IsOpaque
        {
            get
            {
                return _IsOpaque;
            }
            set
            {
                if (_IsOpaque != value)
                {
                    _IsOpaque = value;
                    OnPropertyChanged("IsOpaque");
                    /*if (value)
                    {
                        this.Opacity = 1;
                    }
                    else
                    {
                        this.Opacity = 0.3;
                    }*/
                }
            }
        }

        public ControlConnection(ControlConnector source, ControlConnector sink)
            : base(source, sink)
        {
            EdgeCount = 1;
            IsSelectable = false;
            this.IsOpaque = false;
            this.GotFocus += ControlConnection_GotFocus;
            this.LostFocus += ControlConnection_LostFocus;
        }

        void ControlConnection_GotFocus(object sender, RoutedEventArgs e)
        {
            this.IsOpaque = true;
        }

        void ControlConnection_LostFocus(object sender, RoutedEventArgs e)
        {
            this.IsOpaque = false;
        }

        public void IncrementEdgeCount()
        {
            ++EdgeCount;
        }
        public void DecrementEdgeCount()
        {
            --EdgeCount;
            if (EdgeCount == 0)
            {
                DesignerCanvas canvas = (Application.Current.MainWindow as MainWindow).designerCanvas;
                canvas.Children.Remove(this);
                this.Source = null;
                this.Sink = null;
            }
        }

        override protected void UpdatePathGeometry()
        {
            if (Source != null && Sink != null)
            {
                PathGeometry geometry = new PathGeometry();
                List<Point> linePoints = ControlPathFinder.GetConnectionLine((Source as ControlConnector).GetControlInfo(), (Sink as ControlConnector).GetControlInfo(), true);
                                
                if (linePoints.Count > 0)
                {
                    PathFigure figure = new PathFigure();
                    figure.StartPoint = linePoints[0];
                    linePoints.Remove(linePoints[0]);
                    if (linePoints.Count > 1)
                    {
                        figure.Segments.Add(new PolyBezierSegment(linePoints, true));
                    }
                    else
                    {
                        figure.Segments.Add(new PolyLineSegment(linePoints, true));
                    }
                    geometry.Figures.Add(figure);

                    this.PathGeometry = geometry;
                }                
            }
        }

        protected override void UpdateAnchorPosition()
        {
            try
            {
                base.UpdateAnchorPosition();
                Point pathMidPoint, pathTangentAtMidPoint;
                this.PathGeometry.GetPointAtFractionLength(0.5, out pathMidPoint, out pathTangentAtMidPoint);

                // get angle from tangent vector
                this.ArrowCenterPosition = pathMidPoint;
                this.ArrowCenterAngle = Math.Atan2(pathTangentAtMidPoint.Y, pathTangentAtMidPoint.X) * (180 / Math.PI);
                if (InverseArrow)
                {
                    this.ArrowCenterAngle += 180;
                }
            }
            catch (Exception e)
            {

            }

        }

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (IsSelectable)
            {
                base.OnMouseDown(e);
            }
        }

        protected override void ShowAdorner()
        {            
        }

        protected override void HideAdorner()
        {            
        }
    }

    internal class ControlPathFinder: PathFinder
    {
        internal static List<Point> GetConnectionLine(ControlConnectorInfo source, ControlConnectorInfo sink, bool showLastLine)
        {
            List<Point> linePoints = new List<Point>();

            //Rect rectSource = GetRectWithMargin(source, margin);
            //Rect rectSink = GetRectWithMargin(sink, margin);

            Point startPoint = source.Position; // GetOffsetPoint(source, rectSource);
            Point endPoint = sink.Position; //GetOffsetPoint(sink, rectSink);
            
            linePoints.Add(startPoint);

            if (startPoint.Y != endPoint.Y && startPoint.X != endPoint.X)
            {
                Point midPoint = new Point((startPoint.X + endPoint.X) / 2, (startPoint.Y + endPoint.Y) / 2);
                linePoints.Add(new Point(startPoint.X, midPoint.Y));
                linePoints.Add(new Point(endPoint.X, midPoint.Y));
            }

            linePoints.Add(endPoint);

            //linePoints = OptimizeLinePoints(linePoints, new Rect[] { rectSource, rectSink }, source.Orientation, sink.Orientation);

            //CheckPathEnd(source, sink, showLastLine, linePoints);
            return linePoints;
        }   
    }
}
