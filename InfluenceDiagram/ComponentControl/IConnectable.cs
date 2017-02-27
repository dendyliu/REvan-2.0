using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DiagramDesigner;

namespace InfluenceDiagram.ComponentControl
{
    public interface IConnectable
    {
        ControlConnector connector { get; }
        bool IsFocused { get; }
    }
}
