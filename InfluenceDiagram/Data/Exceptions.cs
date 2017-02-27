using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfluenceDiagram.Data
{
    public class DataException: Exception
    {
        public DataException(string message)
            : base(message)
        {

        }
    }
}
