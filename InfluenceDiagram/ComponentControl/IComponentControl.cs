using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfluenceDiagram.Data;

namespace InfluenceDiagram.ComponentControl
{
    interface IComponentControl
    {
        RootComponentData rootData { get; }
        IConnectable GetConnector(string variableId);
    }
}
