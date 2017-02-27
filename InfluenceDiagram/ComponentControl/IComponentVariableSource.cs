using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfluenceDiagram.Data;
using System.Windows;
using DiagramDesigner;

namespace InfluenceDiagram.ComponentControl
{
    public interface IComponentVariableSource: IConnectable
    {
        AbstractComponentData GetData();
    }
}
