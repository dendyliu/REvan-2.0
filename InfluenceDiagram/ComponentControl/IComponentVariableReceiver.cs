using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfluenceDiagram.ComponentControl
{
    public interface IComponentVariableReceiver
    {
        bool ReceiveComponentVariable(IComponentVariableSource component);
    }
}
