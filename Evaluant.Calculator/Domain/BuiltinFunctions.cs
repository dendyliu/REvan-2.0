using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic;
using MathNet.Numerics.Statistics;

namespace NCalc.Domain
{
    public class BuiltinFunctions
    {
        private static Dictionary<string, AbstractFunction> FunctionRegister = new Dictionary<string, AbstractFunction>()
        {
            {"abs", new Abs()},
            {"and", new And()},
            {"acos", new Acos()},
            {"acosh", new Acosh()},
            {"asin", new Asin()},
            {"asinh", new Asinh()},
            {"atan", new Atan()},
            {"atanh", new Atanh()},
            {"average", new Average()},
            {"ceil", new Ceiling()},
            {"cos", new Cos()},
            {"cosh", new Cosh()},
            {"count", new Count()},
            {"csc", new Csc()},
            {"cot", new Cot()},
            {"exp", new Exp()},
            {"fv", new FV()},
            {"fac", new Factorial()},
            {"floor", new Floor()},
            {"hlookup", new Hlookup()},
            //{"ieeeremainder", new IEEERemainder()},
            {"if", new If()},
            //{"in", new In()},
            {"irr", new IRR()},
            {"ln", new Ln()},
            {"log", new Log()},
            {"log2", new Log2()},
            {"log10", new Log10()},
            {"max", new Max()},
            {"median", new Median()},
            {"min", new Min()},
            {"mode", new Mode()},
            {"npv", new NPV()},
            {"or", new Or()},
            {"pmt", new PMT()},
            {"pow", new Pow()},
            {"pow2", new Pow2()},
            {"pow10", new Pow10()},
            {"pv", new PV()},
            {"rand", new Rand()},
            {"rate", new Rate()},
            {"round", new Round()},
            {"sec", new Sec()},
            {"sigma", new Sigma()},
            {"sign", new Sign()},
            {"sin", new Sin()},
            {"sinh", new Sinh()},
            {"sln", new SLN()},
            {"stdev", new StandardDeviation()},
            {"sum", new Sum()},
            {"sqrt", new Sqrt()},
            {"tan", new Tan()},
            {"tanh", new Tanh()},
            //{"truncate", new Truncate()},
            {"vdb", new VDB()},
            {"vlookup", new Vlookup()},
        };

        public static Dictionary<string, AbstractFunction> AllFunctions {
            get {return FunctionRegister;}
        }

        public static bool Has(string name){            
            return FunctionRegister.ContainsKey(name);
        }

        public static void Validate(string name, Function function)
        {
            AbstractFunction f = FunctionRegister[name];
            f.Validate(function);
        }

        public static object ValidateAndEvaluate(string name, Function function, EvaluationVisitor visitor)
        {
            AbstractFunction f = FunctionRegister[name];
            f.Validate(function);
            return f.Evaluate(function, visitor);
        }

        public static string[][] TempVariables(string name)
        {
            AbstractFunction f = FunctionRegister[name];
            return f.TempVariables();
        }
    }

    #region AbstractFunction

    public abstract class AbstractFunction
    {
        protected void ValidateNumberOfParameter(Function function, int number){
            if (function.Expressions.Length != number)
                throw new ArgumentException(this.GetType().Name + "() takes exactly" + number + " argument");
        }
        abstract public void Validate(Function function);
        abstract public object Evaluate(Function function, EvaluationVisitor visitor);
        virtual public string Description { get { return null; } }
        virtual public string[][] TempVariables() { return null; }
    }

    abstract class NoParamFunction : AbstractFunction
    {
        public override void Validate(Function function)
        {
            ValidateNumberOfParameter(function, 0);
        }
    }

    abstract class UnaryFunction: AbstractFunction
    {
        public override void Validate(Function function)
        {
            ValidateNumberOfParameter(function, 1);              
        }
    }

    abstract class BinaryFunction: AbstractFunction
    {
        public override void Validate(Function function)
        {
            ValidateNumberOfParameter(function, 2);
        }
    }

    abstract class TernaryFunction : AbstractFunction
    {
        public override void Validate(Function function)
        {
            ValidateNumberOfParameter(function, 3);
        }
    }

    abstract class RangeFunction : AbstractFunction
    {
        public override void Validate(Function function)
        {
            // default: must have at least 1 argument
            if (!(function.Expressions.Length >= 1))
                throw new ArgumentException(this.GetType().Name + "() takes at least 1 argument");
        }
        protected List<List<object>> GetOneParameterAsTable(Function function, EvaluationVisitor visitor, int index)
        {
            object parameter = visitor.Evaluate(function.Expressions[index]);
            if (parameter is List<List<object>>)
            {
                return parameter as List<List<object>>;
            }
            else
            {
                List<List<object>> table = new List<List<object>>();
                List<object> row = new List<object>();
                row.Add(parameter);
                table.Add(row);
                return table;
            }
        }
        protected List<object> GetOneParameterAsArray(Function function, EvaluationVisitor visitor, int index)
        {
            List<object> array = new List<object>();
            object parameter = visitor.Evaluate(function.Expressions[index]);
            if (parameter is List<List<object>>)
            {
                List<List<object>> table = parameter as List<List<object>>;
                foreach (List<object> row in table)
                {
                    array.AddRange(row);
                }
            }
            else
            {
                array.Add(parameter);
            }
            return array;
        }
        protected List<object> CombineParametersAsArray(Function function, EvaluationVisitor visitor, int fromIndex = 0)
        {
            List<object> array = new List<object>();
            for (int i = fromIndex; i < function.Expressions.Length; ++i)
            {
                object parameter = visitor.Evaluate(function.Expressions[i]);
                if (parameter is List<List<object>>)
                {
                    List<List<object>> table = parameter as List<List<object>>;
                    foreach (List<object> row in table)
                    {
                        array.AddRange(row);
                    }
                }
                else
                {
                    array.Add(parameter);
                }
            }
            return array;
        }
        protected List<double> FilterArrayByNumber(List<object> array)
        {            
            List<double> newArray = new List<double>();
            foreach (object obj in array)
            {
                try
                {
                    if (obj != null)
                    {
                        double number = Convert.ToDouble(obj);
                        newArray.Add(number);
                    }
                }
                catch (Exception e) { }
            }
            return newArray;
        }
        protected List<double> CombineParametersFilteredAsArrayOfNumber(Function function, EvaluationVisitor visitor, int fromIndex = 0)
        {
            return FilterArrayByNumber(CombineParametersAsArray(function, visitor, fromIndex));
        }
        protected List<double> GetOneParameterFilteredAsArrayOfNumber(Function function, EvaluationVisitor visitor, int index)
        {
            return FilterArrayByNumber(GetOneParameterAsArray(function, visitor, index));
        }
    }

    abstract class LookupFunction : RangeFunction
    {
        protected bool CheckMatch(object a, object b)
        {
            if (a is string && b is string)
            {
                // string comparison is case insensitive
                return (a as string).ToLower().Equals((b as string).ToLower());
            }
            else
            {
                try
                {
                    return Convert.ToDecimal(a).Equals(Convert.ToDecimal(b));
                }
                catch (Exception e) { }
            }
            return false;
        }
        // range match cheks the value <= lookup value
        protected bool CheckLessEqual(object obj, object lookup)
        {
            if (lookup is string && obj is string)
            {
                // string comparison is case insensitive
                return String.Compare(obj as string, lookup as string, true) <= 0;
            }
            else
            {
                try
                {
                    bool result = Convert.ToDecimal(obj) <= Convert.ToDecimal(lookup);
                    return result;
                }
                catch (Exception e) { }
            }
            return false;
        }
    }

    #endregion

    #region Unary functions implementation
    
    class Abs : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Abs(Convert.ToDecimal(
                visitor.Evaluate(function.Expressions[0]))
                );
        }
        public override string Description
        {
            get
            {
                return @"ABS(number)
absolute value of a number, a number without its sign";
            }
        }
    }

    class Acos: UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Acos(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"ACOS(number)
arccosine of a number in radians";
            }
        }
    }

    class Acosh : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            double x = Convert.ToDouble(visitor.Evaluate(function.Expressions[0]));
            return Math.Log(x + Math.Sqrt(x * x - 1)); ;
        }
        public override string Description
        {
            get
            {
                return @"ACOSH(number)
inverse hyperbolic cosine of a number";
            }
        }
    }

    class Asin : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Asin(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"ASIN(number)
arcsine of a number in radians";
            }
        }
    }

    class Asinh : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            double x = Convert.ToDouble(visitor.Evaluate(function.Expressions[0]));
            return Math.Log(x + Math.Sqrt(x * x + 1)); ;
        }
        public override string Description
        {
            get
            {
                return @"ASINH(number)
inverse hyperbolic sine of a number";
            }
        }
    }

    class Atan : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Atan(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"ATAN(number)
arctangent of a number in radians";
            }
        }
    }

    class Atanh : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            double x = Convert.ToDouble(visitor.Evaluate(function.Expressions[0]));
            return Math.Log((1+x)/(1-x))/2;
        }
        public override string Description
        {
            get
            {
                return @"ATANH(number)
inverse hyperbolic tangent of a number";
            }
        }
    }

    class Ceiling : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Ceiling(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"CEIL(number)
rounds a number up";
            }
        }
    }

    class Cos : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Cos(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"COS(number)
cosine of an angle in radians";
            }
        }
    }

    class Cosh : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Cosh(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"COSH(number)
hyperbolic cosine of a number";
            }
        }
    }

    class Cot : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return 1.0/Math.Tan(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"COT(number)
cotangent of an angle in radians";
            }
        }
    }

    class Csc : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return 1.0 / Math.Sin(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"CSC(number)
cosecant of an angle in radians";
            }
        }
    }

    class Exp : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Exp(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"EXP(number)
e raised to the power of a given number";
            }
        }
    }

    class Factorial : UnaryFunction
    {
        private static double CalculateFactorial(Int32 x)
        {
            if (x == 0) return 1;
            else if (x < 0) return Double.NaN;
            else
            {
                double s = 1;
                int i = x;
                while (i > 1 && !Double.IsInfinity(s))
                {
                    s *= i;
                    --i;
                }
                return s;
            }
        }

        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return CalculateFactorial(Convert.ToInt32(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"FAC(number)
factorial of an integer";
            }
        }
    }

    class Floor : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Floor(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"FLOOR(number)
rounds a number down";
            }
        }
    }

    class Ln : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Log(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"LN(number)
natural logarithm of a number";
            }
        }
    }

    class Log2 : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Log(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])), 2);
        }
        public override string Description
        {
            get
            {
                return @"LOG2(number)
logarithm of a number in base 2";
            }
        }
    }

    class Log10 : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Log10(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"LOG10(number)
logarithm of a number in base 10";
            }
        }
    }

    class Pow2 : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Pow(2, Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"POW2(number)
power of 2 (2^x)";
            }
        }
    }

    class Pow10 : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Pow(10, Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"POW10(number)
power of 10 (10^x)";
            }
        }
    }

    class Sec : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return 1.0 / Math.Cos(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"SEC(number)
secant of an angle in radians";
            }
        }
    }

    class Sign : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Sign(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"SIGN(number)
sign of a number: 1 if the number is positive, 0 if the number is zero, or -1 if the number is negative";
            }
        }
    }

    class Sin : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Sin(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"SIN(number)
sine of an angle in radians";
            }
        }
    }

    class Sinh : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Sinh(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"SINH(number)
hyperbolic sine of a number";
            }
        }
    }

    class Sqrt : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Sqrt(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"SQRT(number)
square root of a number";
            }
        }
    }

    class Tan : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Tan(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"TAN(number)
tangent of an angle in radians";
            }
        }
    }

    class Tanh : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Tanh(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"TANH(number)
hyperbolic tangent of a number";
            }
        }
    }

    class Truncate : UnaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Truncate(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])));
        }
        public override string Description
        {
            get
            {
                return @"TRUNCATE(number)
truncates a number to an integer by removing the decimal or fractional part";
            }
        }
    }

    #endregion

    #region Binary function implementation

    class IEEERemainder : BinaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.IEEERemainder(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])), Convert.ToDouble(visitor.Evaluate(function.Expressions[1])));
        }
    }

    class Log : BinaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Log(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])), Convert.ToDouble(visitor.Evaluate(function.Expressions[1])));
        }
        public override string Description
        {
            get
            {
                return @"LOG(number,base)
logarithm of a number to the base you specify";
            }
        }
    }

    class Pow : BinaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Math.Pow(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])), Convert.ToDouble(visitor.Evaluate(function.Expressions[1])));
        }
        public override string Description
        {
            get
            {
                return @"POW(number,power)
raise a number to a power";
            }
        }
    }

    #endregion

    #region Function with other number of arguments
    
    class Round : AbstractFunction
    {
        public override void Validate(Function function)
        {
            if (!(function.Expressions.Length == 2 || function.Expressions.Length == 1))
                throw new ArgumentException("Round() takes 1 or 2 arguments");            
        }
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            int digits = 0;
            if (function.Expressions.Length == 2)
            {
                digits = Convert.ToInt16(visitor.Evaluate(function.Expressions[1]));
            }

            MidpointRounding rounding = (visitor.Options & EvaluateOptions.RoundAwayFromZero) == EvaluateOptions.RoundAwayFromZero ? MidpointRounding.AwayFromZero : MidpointRounding.ToEven;

            return Math.Round(Convert.ToDouble(visitor.Evaluate(function.Expressions[0])), digits, rounding);
        }
        public override string Description
        {
            get
            {
                return @"ROUND(number,[digits=0])
rounds a number to a specified number of digits.";
            }
        }
    }

    class If : AbstractFunction
    {
        public override void Validate(Function function)
        {
            if (function.Expressions.Length != 3)
                throw new ArgumentException("if() takes exactly 3 arguments");
        }
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            bool cond = Convert.ToBoolean(visitor.Evaluate(function.Expressions[0]));

            return cond ? visitor.Evaluate(function.Expressions[1]) : visitor.Evaluate(function.Expressions[2]);
        }
        public override string Description
        {
            get
            {
                return @"IF(condition,value_true,value_false)
checks whether a condition is met, and returns one value if TRUE, and another value if FALSE";
            }
        }
    }

    class In : AbstractFunction
    {
        public override void Validate(Function function)
        {
            if (function.Expressions.Length < 2)
                throw new ArgumentException("in() takes at least 2 arguments");
        }
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            object parameter = visitor.Evaluate(function.Expressions[0]);

            bool evaluation = false;

            // Goes through any values, and stop whe one is found
            for (int i = 1; i < function.Expressions.Length; i++)
            {
                object argument = visitor.Evaluate(function.Expressions[i]);
                if (visitor.CompareUsingMostPreciseType(parameter, argument) == 0)
                {
                    evaluation = true;
                    break;
                }
            }

            return evaluation;
        }
    }

    class Sigma : AbstractFunction
    {
        public override string[][] TempVariables()
        {
            string[][] vars = new string[][]{null, null, new string[]{"k"}};
            return vars;
        }
        public override void Validate(Function function)
        {
            if (function.Expressions.Length != 3)
                throw new ArgumentException("sigma() takes exactly 3 arguments");
        }
        private bool IsValidDouble(double x)
        {
            return !Double.IsNaN(x) && !Double.IsInfinity(x);
        }
        private double DoSum(Function function, EvaluationVisitor visitor)
        {
            double s = 0;
            double k = Convert.ToDouble(visitor.Evaluate(function.Expressions[0]));
            visitor.Parameters["k"] = k;
            if (!IsValidDouble(k)) return Double.NaN;

            double stop = Convert.ToDouble(visitor.Evaluate(function.Expressions[1]));
            if (!IsValidDouble(stop)) return Double.NaN;
            while (k <= stop)
            {
                double plus = Convert.ToDouble(visitor.Evaluate(function.Expressions[2]));
                if (!IsValidDouble(plus)) return Double.NaN;
                s += plus;
                if (!IsValidDouble(s)) break;    // just return whatever s has, NaN or Infinity
                ++k;
                visitor.Parameters["k"] = k;
                stop = Convert.ToDouble(visitor.Evaluate(function.Expressions[1]));
                if (!IsValidDouble(stop)) return Double.NaN;
            }

            return s;
        }
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            double sum = DoSum(function, visitor);
            visitor.Parameters.Remove("k");
            return sum;
        }
        public override string Description
        {
            get
            {
                return @"SIGMA(start,condition,function)
this function will sum the results of the function parameter (3rd), by interating that function using incrementing values of k. k will iterate from the start parameter (1st) to the stop parameter (2nd)";
            }
        }
    }

    class And : AbstractFunction
    {
        public override void Validate(Function function)
        {
            if (!(function.Expressions.Length >= 2))
                throw new ArgumentException("AND() takes at least 2 arguments");
        }
        public override object Evaluate(Function function, EvaluationVisitor visitor)
        {
            foreach (LogicalExpression expression in function.Expressions)
            {
                object evaluate = visitor.Evaluate(expression);
                if (Convert.ToBoolean(evaluate) == false)
                {
                    return false;
                }
            }
            return true;
        }
        public override string Description
        {
            get
            {
                return @"AND(logical1,logical2,...)
checks whether all arguments are TRUE, and returns TRUE if all arguments are TRUE";
            }
        }
    }

    class Or : AbstractFunction
    {
        public override void Validate(Function function)
        {
            if (!(function.Expressions.Length >= 2))
                throw new ArgumentException("OR() takes at least 2 arguments");
        }
        public override object Evaluate(Function function, EvaluationVisitor visitor)
        {
            foreach (LogicalExpression expression in function.Expressions)
            {
                object evaluate = visitor.Evaluate(expression);
                if (Convert.ToBoolean(evaluate) == true)
                {
                    return true;
                }
            }
            return false;
        }
        public override string Description
        {
            get
            {
                return @"OR(logical1,logical2,...)
checks whether any of the arguments are TRUE, and returns FALSE only if all arguments are FALSE";
            }
        }
    }

    #endregion

    #region statistical functions

    class Count : RangeFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            List<double> array = CombineParametersFilteredAsArrayOfNumber(function, visitor);
            return array.Count;
        }
        public override string Description
        {
            get
            {
                return @"COUNT(range)
count the number of cells that contains number";
            }
        }
    }

    class Sum : RangeFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            List<double> array = CombineParametersFilteredAsArrayOfNumber(function, visitor);
            double sum = 0;
            foreach (double obj in array)
            {
                sum += obj;
            }
            return sum;
        }
        public override string Description
        {
            get
            {
                return @"SUM(range)
adds all the numbers in a range of cell";
            }
        }
    }

    class Average : RangeFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            List<double> array = CombineParametersFilteredAsArrayOfNumber(function, visitor);
            return Statistics.Mean(array);            
        }
        public override string Description
        {
            get
            {
                return @"AVERAGE(range)
returns the average of numbers in a range of cell";
            }
        }
    }

    class Max : RangeFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            List<double> array = CombineParametersFilteredAsArrayOfNumber(function, visitor);
            if (array.Count == 0) return 0;

            return Statistics.Maximum(array);
        }
        public override string Description
        {
            get
            {
                return @"MAX(range)
returns the largest number in a set of values";
            }
        }
    }

    class Min : RangeFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            List<double> array = CombineParametersFilteredAsArrayOfNumber(function, visitor);
            if (array.Count == 0) return 0;

            return Statistics.Minimum(array);
        }
        public override string Description
        {
            get
            {
                return @"MIN(range)
returns the smallest number in a set of values";
            }
        }
    }

    class StandardDeviation : RangeFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            List<double> array = CombineParametersFilteredAsArrayOfNumber(function, visitor);
            return Statistics.StandardDeviation(array);
        }
        public override string Description
        {
            get
            {
                return @"STDEV(range)
estimates standard deviation based on a sample";
            }
        }
    }

    class Mode : RangeFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            List<double> array = CombineParametersFilteredAsArrayOfNumber(function, visitor);
            var groupedOrdered = array.GroupBy(v => v).OrderByDescending(g => g.Count());
            if (groupedOrdered.Count() > 1 && groupedOrdered.ElementAt(0).Count() == groupedOrdered.ElementAt(1).Count())
            {
                throw new Exception("MODE: There are multiple mode");
            }
            double mode = groupedOrdered.First().Key;
            return mode;
        }
        public override string Description
        {
            get
            {
                return @"MODE(range)
returns the most frequently occuring value in a range of data";
            }
        }
    }

    class Median : RangeFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            List<double> array = CombineParametersFilteredAsArrayOfNumber(function, visitor);
            return Statistics.Median(array);
        }
        public override string Description
        {
            get
            {
                return @"MEDIAN(range)
returns the median of the set of given numbers";
            }
        }
    }

    class Rand: NoParamFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            Random rand = new Random();
            return rand.NextDouble();
        }
        public override string Description
        {
            get
            {
                return @"RAND()
returns a random number between 0.0 and 1.0";
            }
        }
    }

    #endregion

    #region financial functions

    class IRR : RangeFunction
    {
        public override void Validate(Function function)
        {
            if (!(function.Expressions.Length == 2 || function.Expressions.Length == 1))
                throw new ArgumentException("IRR() takes 1 or 2 arguments");                     
        }
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            List<double> array = GetOneParameterFilteredAsArrayOfNumber(function, visitor, 0);
            double guess = 0.1;
            if (function.Expressions.Length == 2)
            {
                guess = Convert.ToDouble(visitor.Evaluate(function.Expressions[1]));
            }
            double[] array2 = array.ToArray();
            return Financial.IRR(ref array2, guess);            
        }
        public override string Description
        {
            get
            {
                return @"IRR(range,[guess=0.1])
returns the internal rate of return for a series of cashflow";
            }
        }
    }

    class NPV : RangeFunction
    {
        public override void Validate(Function function)
        {
            if (!(function.Expressions.Length >= 2))
                throw new ArgumentException("NPV() takes at least 2 arguments");  
        }
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            double rate = Convert.ToDouble(visitor.Evaluate(function.Expressions[0]));
            List<double> array = CombineParametersFilteredAsArrayOfNumber(function, visitor, 1);
            double[] array2 = array.ToArray();
            return Financial.NPV(rate, ref array2);            
        }
        public override string Description
        {
            get
            {
                return @"NPV(rate,range)
returns the net present value of an investment based on a discount rate and a series of future payments (negative values) and income (positive values)";
            }
        }
    }

    class SLN : TernaryFunction
    {
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            return Financial.SLN(
                Convert.ToDouble(visitor.Evaluate(function.Expressions[0])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[1])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[2]))
                );
        }
        public override string Description
        {
            get
            {
                return @"SLN(cost,salvage,life)
returns the straight-line depreciation of an asset for one period";
            }
        }
    }

    class VDB : TernaryFunction
    {
        public override void Validate(Function function)
        {
            if (!(function.Expressions.Length >= 5 && function.Expressions.Length <= 7))
                throw new ArgumentException("VDB() takes 5 to 7 arguments");
        }
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            double factor = 2;
            Excel.FinancialFunctions.VdbSwitch noswitch = Excel.FinancialFunctions.VdbSwitch.SwitchToStraightLine;
            if (function.Expressions.Length >= 6)
            {
                factor = Convert.ToDouble(visitor.Evaluate(function.Expressions[5]));
            }
            if (function.Expressions.Length >= 7)
            {
                int noswitchInt = Convert.ToInt16(visitor.Evaluate(function.Expressions[6]));
                if (noswitchInt != 0)
                {
                    noswitch = Excel.FinancialFunctions.VdbSwitch.DontSwitchToStraightLine;
                }
            }
            return Excel.FinancialFunctions.Financial.Vdb(
                Convert.ToDouble(visitor.Evaluate(function.Expressions[0])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[1])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[2])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[3])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[4])),
                factor,
                noswitch
                );
        }
        public override string Description
        {
            get
            {
                return @"VDB(cost,salvage,life,start_period,end_period,[factor=2],[no_switch=0])
returns the depreciation of an asset for any period you specify, including partial periods, using the double-declining balance method or some other method you specify";
            }
        }
    }

    class PMT : AbstractFunction
    {
        public override void Validate(Function function)
        {
            if (!(function.Expressions.Length >= 3 && function.Expressions.Length <= 5))
                throw new ArgumentException("PMT() takes 3 to 5 arguments");  
        }
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            double FV = 0;
            DueDate Due = DueDate.EndOfPeriod;
            if (function.Expressions.Length >= 4){
                FV = Convert.ToDouble(visitor.Evaluate(function.Expressions[3]));
            }
            if (function.Expressions.Length >= 5){
                int type = Convert.ToInt16(visitor.Evaluate(function.Expressions[4]));
                if (type != 0){
                    Due = DueDate.BegOfPeriod;
                }
            }
            return Financial.Pmt(
                Convert.ToDouble(visitor.Evaluate(function.Expressions[0])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[1])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[2])),
                FV,
                Due
                );
        }
        public override string Description
        {
            get
            {
                return @"PMT(rate,nper,pv,[fv=0],[type=0])
calculates the payment for a loan based on constant payments and a constant interest rate";
            }
        }
    }

    class Rate : AbstractFunction
    {
        public override void Validate(Function function)
        {
            if (!(function.Expressions.Length >= 3 && function.Expressions.Length <= 6))
                throw new ArgumentException("Rate() takes 3 to 6 arguments");
        }
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            double FV = 0;
            DueDate Due = DueDate.EndOfPeriod;
            double guess = 0.1;
            if (function.Expressions.Length >= 4)
            {
                FV = Convert.ToDouble(visitor.Evaluate(function.Expressions[3]));
            }
            if (function.Expressions.Length >= 5)
            {
                int type = Convert.ToInt16(visitor.Evaluate(function.Expressions[4]));
                if (type != 0)
                {
                    Due = DueDate.BegOfPeriod;
                }
            }
            if (function.Expressions.Length >= 6)
            {
                guess = Convert.ToDouble(visitor.Evaluate(function.Expressions[5]));
            }
            return Financial.Rate(
                Convert.ToDouble(visitor.Evaluate(function.Expressions[0])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[1])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[2])),
                FV,
                Due,
                guess
                );
        }
        public override string Description
        {
            get
            {
                return @"RATE(nper,pmt,pv,[fv=0],[type=0],[guess=0.1])
returns the interest rate per period of a loan or an investment";
            }
        }
    }

    class PV : AbstractFunction
    {
        public override void Validate(Function function)
        {
            if (!(function.Expressions.Length >= 3 && function.Expressions.Length <= 5))
                throw new ArgumentException("PV() takes 3 to 5 arguments");
        }
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            double FV = 0;
            DueDate Due = DueDate.EndOfPeriod;
            if (function.Expressions.Length >= 4)
            {
                FV = Convert.ToDouble(visitor.Evaluate(function.Expressions[3]));
            }
            if (function.Expressions.Length >= 5)
            {
                int type = Convert.ToInt16(visitor.Evaluate(function.Expressions[4]));
                if (type != 0)
                {
                    Due = DueDate.BegOfPeriod;
                }
            }
            return Financial.PV(
                Convert.ToDouble(visitor.Evaluate(function.Expressions[0])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[1])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[2])),
                FV,
                Due
                );
        }
        public override string Description
        {
            get
            {
                return @"PV(rate,nper,pmt,[fv=0],[type=0])
returns the present value of an investment";
            }
        }
    }

    class FV : AbstractFunction
    {
        public override void Validate(Function function)
        {
            if (!(function.Expressions.Length >= 3 && function.Expressions.Length <= 5))
                throw new ArgumentException("FV() takes 3 to 5 arguments");
        }
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            double PV = 0;
            DueDate Due = DueDate.EndOfPeriod;
            if (function.Expressions.Length >= 4)
            {
                PV = Convert.ToDouble(visitor.Evaluate(function.Expressions[3]));
            }
            if (function.Expressions.Length >= 5)
            {
                int type = Convert.ToInt16(visitor.Evaluate(function.Expressions[4]));
                if (type != 0)
                {
                    Due = DueDate.BegOfPeriod;
                }
            }
            return Financial.FV(
                Convert.ToDouble(visitor.Evaluate(function.Expressions[0])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[1])),
                Convert.ToDouble(visitor.Evaluate(function.Expressions[2])),
                PV,
                Due
                );
        }
        public override string Description
        {
            get
            {
                return @"FV(rate,nper,pmt,[pv=0],[type=0])
returns the future value of an investment";
            }
        }
    }

    #endregion

    #region reference function

    class Hlookup : LookupFunction
    {
        public override void Validate(Function function)
        {
            if (!(function.Expressions.Length >= 3 && function.Expressions.Length <= 4))
                throw new ArgumentException("HLOOKUP() takes 3 to 4 arguments");
        }
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            object value = visitor.Evaluate(function.Expressions[0]);
            List<List<object>> table = GetOneParameterAsTable(function, visitor, 1);
            // row num starts at 1, not 0
            int rowNum = Convert.ToInt16(visitor.Evaluate(function.Expressions[2]));
            // if rangeLookup is true, find the last value which is <= lookup value (items are assumed to be sorted ascending)
            bool rangeLookup = true;
            if (function.Expressions.Length >= 4)
            {
                int rangeLookupInt = Convert.ToInt16(visitor.Evaluate(function.Expressions[3]));
                rangeLookup = rangeLookupInt != 0;
            }
            // the row num should be <= the row count
            if (rowNum < 1 || rowNum > table.Count)
            {
                throw new Exception("HLOOKUP: row_num is not within acceptable range");
            }
            object rangePrevCompare = null;
            for (int col = 0; col < table[0].Count; ++col)
            {
                if (rangeLookup)
                {
                    // range lookup logic: first find 1 value that's <= lookup value
                    // then check that the next items are ascending until meeting an item that's > lookup value
                    // if found, return the element before it
                    // if value that's > not found, then the last item has matched
                    object obj = table[0][col];
                    bool isLowerOrEqual = false;
                    if (rangePrevCompare == null)
                    {
                        isLowerOrEqual = CheckLessEqual(obj, value);
                    }
                    else
                    {
                        // compare with lookup and also the previous item (to make sure the ascending sequence)
                        isLowerOrEqual = CheckLessEqual(obj, value) && CheckLessEqual(rangePrevCompare, obj);
                    }
                    if (isLowerOrEqual)
                    {
                        rangePrevCompare = obj;
                    }
                    else
                    {
                        if (rangePrevCompare != null)
                        {
                            object result = table[rowNum - 1][col-1];
                            if (result == null) return 0;    // the default behavior in excel is returning 0 when the matched cell is blank
                            else return result;
                        }
                    }
                }
                else
                {
                    bool match = CheckMatch(value, table[0][col]);
                    if (match)
                    {
                        object result = table[rowNum - 1][col];
                        if (result == null) return 0;    // the default behavior in excel is returning 0 when the matched cell is blank
                        else return result;
                    }
                }
            }
            // value not found
            if (rangeLookup && rangePrevCompare != null)
            {
                // last item match
                object result = table[rowNum - 1][table[0].Count - 1];
                if (result == null) return 0;    // the default behavior in excel is returning 0 when the matched cell is blank
                else return result;
            }
            throw new Exception("HLOOKUP: value is not found");
        }
        public override string Description
        {
            get
            {
                return @"HLOOKUP(value,table,row_num,[range_lookup=1])
looks for a value in the top row of a table and returns the value in the same column from a row you specify";
            }
        }
    }

    class Vlookup : LookupFunction
    {
        public override void Validate(Function function)
        {
            if (!(function.Expressions.Length >= 3 && function.Expressions.Length <= 4))
                throw new ArgumentException("VLOOKUP() takes 3 to 4 arguments");
        }
        override public object Evaluate(Function function, EvaluationVisitor visitor)
        {
            object value = visitor.Evaluate(function.Expressions[0]);
            List<List<object>> table = GetOneParameterAsTable(function, visitor, 1);
            // col num starts at 1, not 0
            int colNum = Convert.ToInt16(visitor.Evaluate(function.Expressions[2]));
            // if rangeLookup is true, find the last value which is <= lookup value (items are assumed to be sorted ascending)
            bool rangeLookup = true;
            if (function.Expressions.Length >= 4)
            {
                int rangeLookupInt = Convert.ToInt16(visitor.Evaluate(function.Expressions[3]));
                rangeLookup = rangeLookupInt != 0;
            }
            // the row num should be <= the col count
            if (colNum < 1 || colNum > table[0].Count)
            {
                throw new Exception("VLOOKUP: col_num is not within acceptable range");
            }
            object rangePrevCompare = null;
            for (int row = 0; row < table.Count; ++row)
            {
                if (rangeLookup)
                {
                    // range lookup logic: first find 1 value that's <= lookup value
                    // then check that the next items are ascending until meeting an item that's > lookup value
                    // if found, return the element before it
                    // if value that's > not found, then the last item has matched
                    object obj = table[row][0];
                    bool isLowerOrEqual = false;
                    if (rangePrevCompare == null)
                    {
                        isLowerOrEqual = CheckLessEqual(obj, value);
                    }
                    else
                    {
                        // compare with lookup and also the previous item (to make sure the ascending sequence)
                        isLowerOrEqual = CheckLessEqual(obj, value) && CheckLessEqual(rangePrevCompare, obj);
                    }
                    if (isLowerOrEqual)
                    {
                        rangePrevCompare = obj;
                    }
                    else
                    {
                        if (rangePrevCompare != null)
                        {
                            object result = table[row - 1][colNum - 1];
                            if (result == null) return 0;    // the default behavior in excel is returning 0 when the matched cell is blank
                            else return result;
                        }
                    }
                }
                else
                {
                    bool match = CheckMatch(value, table[row][0]);
                    if (match)
                    {
                        object result = table[row][colNum - 1];
                        if (result == null) return 0;    // the default behavior in excel is returning 0 when the matched cell is blank
                        else return result;
                    }
                }
            }
            // value not found
            if (rangeLookup && rangePrevCompare != null)
            {
                // last item match
                object result = table[table.Count - 1][colNum - 1];
                if (result == null) return 0;    // the default behavior in excel is returning 0 when the matched cell is blank
                else return result;
            }
            throw new Exception("VLOOKUP: value is not found");
        }
        public override string Description
        {
            get
            {
                return @"VLOOKUP(value,table,col_num,[range_lookup=1])
looks for a value in the leftmost column of a table and returns the value in the same row from a column you specify";
            }
        }
    }

    #endregion

}
