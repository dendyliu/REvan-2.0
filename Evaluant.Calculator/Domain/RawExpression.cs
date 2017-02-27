using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NCalc.Domain
{
    public class RawExpression: LogicalExpression
    {        
        public string Expression { get; set; }

        public RawExpression(string expression)
        {
            this.Expression = expression;
        }

        public override void Accept(LogicalExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    class RawExpressionException : Exception
    {

    }
}
