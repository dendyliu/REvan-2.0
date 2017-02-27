using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace InfluenceDiagram.Data
{
    public enum ComponentShape
    {
        [Description("../Resources/Images/shape-rectrounded1.png")]
        RectangleRounded1,  // big corner radius
        [Description("../Resources/Images/shape-rectrounded2.png")]
        RectangleRounded2,  // little corner radius
        [Description("../Resources/Images/shape-rect.png")]
        Rectangle,
        [Description("../Resources/Images/shape-hexagon.png")]
        Hexagon,
        [Description("../Resources/Images/shape-octagon.png")]
        Octagon
    }
}
