﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DiagramDesigner.Controls;
using InfluenceDiagram.ComponentControl;
using InfluenceDiagram;
using InfluenceDiagram.Utility;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Collections.Generic;

namespace DiagramDesigner
{
    //These attributes identify the types of the named parts that are used for templating
    [TemplatePart(Name = "PART_DragThumb", Type = typeof(DragThumb))]
    [TemplatePart(Name = "PART_ResizeDecorator", Type = typeof(Control))]
    [TemplatePart(Name = "PART_ConnectorDecorator", Type = typeof(Control))]
    [TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
    public class DesignerItem : ContentControl, ISelectable, IGroupable
    {
        #region ID
        private Guid id;
        public Guid ID
        {
            get { return id; }
        }
        #endregion

        #region ParentID
        public Guid ParentID
        {
            get { return (Guid)GetValue(ParentIDProperty); }
            set { SetValue(ParentIDProperty, value); }
        }
        public static readonly DependencyProperty ParentIDProperty = DependencyProperty.Register("ParentID", typeof(Guid), typeof(DesignerItem));
        #endregion

        #region IsGroup
        public bool IsGroup
        {
            get { return (bool)GetValue(IsGroupProperty); }
            set { SetValue(IsGroupProperty, value); }
        }
        public static readonly DependencyProperty IsGroupProperty =
            DependencyProperty.Register("IsGroup", typeof(bool), typeof(DesignerItem));
        #endregion
        
        #region IsSelected Property

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set {
                SetValue(IsSelectedProperty, value);
            }
        }
        public static readonly DependencyProperty IsSelectedProperty =
          DependencyProperty.Register("IsSelected",
                                       typeof(bool),
                                       typeof(DesignerItem),
                                       new FrameworkPropertyMetadata(false));

        #endregion

        #region DragThumbTemplate Property

        // can be used to replace the default template for the DragThumb
        public static readonly DependencyProperty DragThumbTemplateProperty =
            DependencyProperty.RegisterAttached("DragThumbTemplate", typeof(ControlTemplate), typeof(DesignerItem));

        public static ControlTemplate GetDragThumbTemplate(UIElement element)
        {
            return (ControlTemplate)element.GetValue(DragThumbTemplateProperty);
        }

        public static void SetDragThumbTemplate(UIElement element, ControlTemplate value)
        {
            element.SetValue(DragThumbTemplateProperty, value);
        }

        #endregion

        #region ConnectorDecoratorTemplate Property

        // can be used to replace the default template for the ConnectorDecorator
        public static readonly DependencyProperty ConnectorDecoratorTemplateProperty =
            DependencyProperty.RegisterAttached("ConnectorDecoratorTemplate", typeof(ControlTemplate), typeof(DesignerItem));

        public static ControlTemplate GetConnectorDecoratorTemplate(UIElement element)
        {
            return (ControlTemplate)element.GetValue(ConnectorDecoratorTemplateProperty);
        }

        public static void SetConnectorDecoratorTemplate(UIElement element, ControlTemplate value)
        {
            element.SetValue(ConnectorDecoratorTemplateProperty, value);
        }

        #endregion

        #region IsDragConnectionOver

        // while drag connection procedure is ongoing and the mouse moves over 
        // this item this value is true; if true the ConnectorDecorator is triggered
        // to be visible, see template
        public bool IsDragConnectionOver
        {
            get { return (bool)GetValue(IsDragConnectionOverProperty); }
            set { SetValue(IsDragConnectionOverProperty, value); }
        }
        public static readonly DependencyProperty IsDragConnectionOverProperty =
            DependencyProperty.Register("IsDragConnectionOver",
                                         typeof(bool),
                                         typeof(DesignerItem),
                                         new FrameworkPropertyMetadata(false));

        #endregion

        private Point dragStartPosition;

        static DesignerItem()
        {
            // set the key to reference the style for this control
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DesignerItem), new FrameworkPropertyMetadata(typeof(DesignerItem)));
        }

        public DesignerItem(Guid id)
        {
            this.id = id;
            this.Loaded += new RoutedEventHandler(DesignerItem_Loaded);
        }

        public DesignerItem()
            : this(Guid.NewGuid())
        {
        }

        public void ReceiveClick()
        {
            DesignerCanvas designer = VisualTreeHelper.GetParent(this) as DesignerCanvas;

            // update selection
            if (designer != null)
            {
                if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != ModifierKeys.None)
                {
                    if (this.IsSelected)
                    {
                        designer.SelectionService.RemoveFromSelection(this);
                    }
                    else
                    {
                        designer.SelectionService.AddToSelection(this);
                    }
                }
                else
                {
                    if (!this.IsSelected)
                    {
                        designer.SelectionService.SelectItem(this);
                        RoutedUICommand command = Command.ClickComponent;
                        command.Execute(this, Application.Current.MainWindow);
                    }
                }
                //Focus();
            }

        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            ReceiveClick();
            e.Handled = false;
        }

        void DesignerItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (base.Template != null)
            {
                ContentPresenter contentPresenter =
                    this.Template.FindName("PART_ContentPresenter", this) as ContentPresenter;
                if (contentPresenter != null)
                {
                    UIElement contentVisual = VisualTreeHelper.GetChild(contentPresenter, 0) as UIElement;
                    if (contentVisual != null)
                    {
                        DragThumb thumb = this.Template.FindName("PART_DragThumb", this) as DragThumb;
                        if (thumb != null)
                        {
                            ControlTemplate template =
                                DesignerItem.GetDragThumbTemplate(contentVisual) as ControlTemplate;
                            if (template != null)
                                thumb.Template = template;
                            thumb.DragStarted += thumb_DragStarted;
                            thumb.DragCompleted += thumb_DragCompleted;
                        }
                    }
                }
            }
        }


        public event ComponentDragCompletedEventHandler ComponentDragCompleted;

        void thumb_DragStarted(object sender, DragStartedEventArgs e)
        {            
            ScrollViewer scrollViewer = UIHelper.FindVisualParent<ScrollViewer>(this);
            double left = Canvas.GetLeft(this);
            double top = Canvas.GetTop(this);
            dragStartPosition = new Point(left, top);
        }

        void thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            ScrollViewer scrollViewer = UIHelper.FindVisualParent<ScrollViewer>(this);
            double left = Canvas.GetLeft(this);
            double top = Canvas.GetTop(this);
            Point dragEndPosition = new Point(left, top);

            if (!(dragEndPosition.Equals(dragStartPosition)))
            {
                if (this.Content is IComponentControl)
                {
                    ComponentDragCompletedEventArgs args = new ComponentDragCompletedEventArgs()
                    {
                        component = this.Content,
                        startPosition = dragStartPosition,
                        endPosition = dragEndPosition
                    };
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                     new Action(delegate()
                     {
                         ComponentDragCompleted(this, args);
                     }));
                }
            }
        }

        public void AddContextMenuItems(List<MenuItem> items)
        {
            Grid grid = UIHelper.FindVisualChild<Grid>(this);
            ContextMenu contextMenu = grid.ContextMenu;
            // recreate the context menu because the original context menu is a shared resource
            ContextMenu newContextMenu = new ContextMenu();
            foreach (MenuItem item in contextMenu.Items)
            {
                MenuItem newItem = new MenuItem()
                {
                    Header = item.Header,
                    Command = item.Command,
                    Icon = item.Icon
                };
                newContextMenu.Items.Add(newItem);
            }
            foreach (MenuItem newItem in items)
            {
                newContextMenu.Items.Add(newItem);
            }
            grid.ContextMenu = newContextMenu;
        }
    }
}
