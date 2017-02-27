using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NCalc.Domain
{
    public class RawIdentifierExpression: LogicalExpression
    {
        public string Identifier { get; set; }

        public RawIdentifierExpression(string identifier)
        {
            this.Identifier = identifier;
        }

        public override void Accept(LogicalExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
