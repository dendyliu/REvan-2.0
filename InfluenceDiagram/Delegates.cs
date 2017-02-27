using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace InfluenceDiagram
{
    // passed from WorksheetData when there's change that should be reflected on Control 
    // (e.g. propagating new values after evaluation)
    public delegate void ComponentDataChangedEventHandler(object sender, ComponentDataChangedEventArgs e);
    public delegate void ComponentDependenceAddedEventHandler(object sender, ComponentDependenceEventArgs e);
    public delegate void ComponentDependenceRemovedEventHandler(object sender, ComponentDependenceEventArgs e);
    public delegate void ComponentPropertyChangedEventHandler(object sender, ComponentPropertyChangedEventArgs e);
    public delegate void ComponentDragCompletedEventHandler(object sender, ComponentDragCompletedEventArgs e);

    public class ComponentDataChangedEventArgs: EventArgs
    {
        public string variableId { get; set; }
    }

    public class ComponentDependenceEventArgs : EventArgs
    {
        public string sourceVariableId { get; set; }
        public string targetVariableId { get; set; }
    }

    public class ComponentPropertyChangedEventArgs : EventArgs
    {
        public string VariableId { get; set; }
        public Dictionary<string,object> OldProperties { get; set; }
        public Dictionary<string,object> NewProperties { get; set; }
    }

    public class ComponentDragCompletedEventArgs : EventArgs
    {
        public object component { get; set; }
        public Point startPosition { get; set; }
        public Point endPosition { get; set; }
    }
}
