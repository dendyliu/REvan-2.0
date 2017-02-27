using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace NCalc.Domain
{
    public class RawExpressionVisitor: LogicalExpressionVisitor
    {
        private readonly NumberFormatInfo _numberFormatInfo;
        private readonly EvaluateOptions _options = EvaluateOptions.None;
        private bool IgnoreCase { get { return (_options & EvaluateOptions.IgnoreCase) == EvaluateOptions.IgnoreCase; } }

        public RawExpressionVisitor(EvaluateOptions options)
        {
            _options = options;
            Result = new StringBuilder();
            _numberFormatInfo = new NumberFormatInfo {NumberDecimalSeparator = "."};
        }

        public StringBuilder Result { get; protected set; }

        public override void Visit(LogicalExpression expression)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void Visit(TernaryExpression expression)
        {
            EncapsulateNoValue(expression.LeftExpression);

            Result.Append("?");

            EncapsulateNoValue(expression.MiddleExpression);

            Result.Append(":");

            EncapsulateNoValue(expression.RightExpression);
        }

        public override void Visit(BinaryExpression expression)
        {
            EncapsulateNoValue(expression.LeftExpression);

            switch (expression.Type)
            {
                case BinaryExpressionType.And:
                    Result.Append("and");
                    break;

                case BinaryExpressionType.Or:
                    Result.Append("or");
                    break;

                case BinaryExpressionType.Div:
                    Result.Append("/");
                    break;

                case BinaryExpressionType.Equal:
                    Result.Append("=");
                    break;

                case BinaryExpressionType.Greater:
                    Result.Append(">");
                    break;

                case BinaryExpressionType.GreaterOrEqual:
                    Result.Append(">=");
                    break;

                case BinaryExpressionType.Lesser:
                    Result.Append("<");
                    break;

                case BinaryExpressionType.LesserOrEqual:
                    Result.Append("<=");
                    break;

                case BinaryExpressionType.Minus:
                    Result.Append("-");
                    break;

                case BinaryExpressionType.Modulo:
                    Result.Append("%");
                    break;

                case BinaryExpressionType.NotEqual:
                    Result.Append("!=");
                    break;

                case BinaryExpressionType.Plus:
                    Result.Append("+");
                    break;

                case BinaryExpressionType.Times:
                    Result.Append("*");
                    break;

                case BinaryExpressionType.BitwiseAnd:
                    Result.Append("&");
                    break;

                case BinaryExpressionType.BitwiseOr:
                    Result.Append("|");
                    break;

                case BinaryExpressionType.BitwiseXOr:
                    Result.Append("~");
                    break;

                case BinaryExpressionType.LeftShift:
                    Result.Append("<<");
                    break;

                case BinaryExpressionType.RightShift:
                    Result.Append(">>");
                    break;
            }

            EncapsulateNoValue(expression.RightExpression);
        }

        public override void Visit(UnaryExpression expression)
        {
            switch (expression.Type)
            {
                case UnaryExpressionType.Not:
                    Result.Append("!");
                    break;

                case UnaryExpressionType.Negate:
                    Result.Append("-");
                    break;

                case UnaryExpressionType.BitwiseNot:
                    Result.Append("~");
                    break;
            }

            EncapsulateNoValue(expression.Expression);
        }

        public override void Visit(ValueExpression expression)
        {
            switch (expression.Type)
            {
                case ValueType.Boolean:
                    Result.Append(expression.Value.ToString());//.Append(" ");
                    break;

                case ValueType.DateTime:
                    Result.Append("#").Append(expression.Value.ToString()).Append("#");//.Append(" ");
                    break;

                case ValueType.Float:
                    Result.Append(decimal.Parse(expression.Value.ToString()).ToString(_numberFormatInfo));//.Append(" ");
                    break;

                case ValueType.Integer:
                    Result.Append(expression.Value.ToString());//.Append(" ");
                    break;

                case ValueType.String:
                    Result.Append("'").Append(expression.Value.ToString()).Append("'");//.Append(" ");
                    break;
            }
        }

        public override void Visit(Function function)
        {
            var args = new FunctionArgs
            {
                Parameters = new Expression[function.Expressions.Length]
            };

            // Don't call parameters right now, instead let the function do it as needed.
            // Some parameters shouldn't be called, for instance, in a if(), the "not" value might be a division by zero
            // Evaluating every value could produce unexpected behaviour
            for (int i = 0; i < function.Expressions.Length; i++)
            {
                args.Parameters[i] = new Expression(function.Expressions[i], _options);
                args.Parameters[i].EvaluateFunction += EvaluateFunction;
                args.Parameters[i].EvaluateParameter += EvaluateParameter;

                // Assign the parameters of the Expression to the arguments so that custom Functions and Parameters can use them
                args.Parameters[i].Parameters = Parameters;
            }

            // Calls external implementation
            bool found = OnValidateFunction(IgnoreCase ? function.Identifier.Name.ToLower() : function.Identifier.Name, args);
            string[][] tempVariables = null;
            // validate builtin function
            if (!found)
            {
                string functionName = function.Identifier.Name.ToLower();
                if (BuiltinFunctions.Has(functionName))
                {
                    BuiltinFunctions.Validate(functionName, function);
                    found = true;
                    tempVariables = BuiltinFunctions.TempVariables(functionName);
                }
            }

            if (!found)
            {
                // function not found OR parameter is invalid
                throw new Exception("Function \""+function.Identifier.Name+"\" not defined");
            }

            Result.Append(function.Identifier.Name);

            Result.Append("(");

            for(int i=0; i<function.Expressions.Length; i++)
            {
                string[] tempVars = null;
                if (tempVariables != null && i < tempVariables.Length)
                {
                    tempVars = tempVariables[i];
                    if (tempVars != null)
                    {
                        foreach (string var in tempVars)
                        {
                            this.Parameters[var] = new RawIdentifierExpression(var);
                        }
                    }
                }
                function.Expressions[i].Accept(this);
                if (tempVars != null)
                {
                    foreach (string var in tempVars)
                    {
                        this.Parameters.Remove(var);
                    }
                }
                if (i < function.Expressions.Length-1)
                {
                    Result.Append(",");
                }
            }

            // trim spaces before adding a closing paren
            while (Result[Result.Length - 1] == ' ')
                Result.Remove(Result.Length - 1, 1);

            Result.Append(")");
        }

        public override void Visit(Identifier parameter)
        {
            object parameterValue;
            if (Parameters.ContainsKey(parameter.Name))
            {
                // The parameter is defined in the hashtable
                if (Parameters[parameter.Name] is Expression)
                {
                    // The parameter is itself another Expression
                    var expression = (Expression)Parameters[parameter.Name];

                    // Overloads parameters 
                    foreach (var p in Parameters)
                    {
                        expression.Parameters[p.Key] = p.Value;
                    }

                    expression.EvaluateFunction += EvaluateFunction;
                    expression.EvaluateParameter += EvaluateParameter;

                    parameterValue = ((Expression)Parameters[parameter.Name]).EvaluateRaw();
                }
                else
                    parameterValue = Parameters[parameter.Name];
            }
            else
            {
                // The parameter should be defined in a call back method
                var args = new ParameterArgs();

                // Calls external implementation
                OnEvaluateParameter(parameter.Name, args);

                if (!args.HasResult)
                    throw new ArgumentException("Parameter was not defined", parameter.Name);
                
                parameterValue = args.Result;
            }

            if (parameterValue is RawIdentifierExpression)
            {
                (parameterValue as RawIdentifierExpression).Accept(this);
            }
            else if (parameterValue is RawExpression)
            {
                (parameterValue as RawExpression).Accept(this);
            }
            else
            {
                //Result.Append("[").Append(parameterValue.ToString()).Append("]");
                Result.Append(parameterValue.ToString());
            }
        }

        public override void Visit(RawExpression expression)
        {
            Result.Append("(").Append(expression.Expression).Append(")");
            //Result.Append(expression.Expression);
        }

        public override void Visit(RawIdentifierExpression expression)
        {
            //Result.Append("[").Append(expression.Identifier).Append("]");
            Result.Append(expression.Identifier);
        }

        protected void EncapsulateNoValue(LogicalExpression expression)
        {
            if (expression is ValueExpression 
                || expression is Identifier 
                || expression is RawIdentifierExpression 
                || expression is Function)
            {
                expression.Accept(this);
            }
            else
            {
                Result.Append("(");
                expression.Accept(this);
                
                // trim spaces before adding a closing paren
                while(Result[Result.Length - 1] == ' ')
                    Result.Remove(Result.Length - 1, 1);
                
                Result.Append(")");
            }
        }

        public event EvaluateFunctionHandler EvaluateFunction;
        public event ValidateFunctionHandler ValidateFunction;

        private bool OnValidateFunction(string name, FunctionArgs args)
        {
            if (ValidateFunction != null)
                return ValidateFunction(name, args);
            else
                return false;
        }

        public event EvaluateParameterHandler EvaluateParameter;

        private void OnEvaluateParameter(string name, ParameterArgs args)
        {
            if (EvaluateParameter != null)
                EvaluateParameter(name, args);
        }

        public Dictionary<string, object> Parameters { get; set; }
    }
}
