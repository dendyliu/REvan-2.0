using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace InfluenceDiagram.Data
{
    public class DataHelper
    {
        public static string FunctionNameRegex = @"^[a-zA-Z][a-zA-Z0-9]*$";
        /** function name must start with alphabet and contains only alphanumerics **/
        public static bool IsValidFunctionName(string function)
        {
            return Regex.IsMatch(function, FunctionNameRegex);
        }

        public static string ParamNameRegex = @"^[a-zA-Z][a-zA-Z0-9]*$";
        /** function name must start with alphabet and contains only alphanumerics **/
        public static bool IsValidParamName(string param)
        {
            return Regex.IsMatch(param, ParamNameRegex);
        }

        public static string ReplaceExpressionVariableId(string expression, string oldId, string newId)
        {
            return Regex.Replace(expression, @"\[" + oldId + @"_(.*)\]", "[" + newId + "_$1]");
        }

        public static MatchCollection MatchesRangeVariable(string expression, string spreadsheetId)
        {
            return Regex.Matches(expression, String.Format(@"\[{0}_([^:]*):{0}_([^\]]*)\]", spreadsheetId));
        }

        public static bool IsSpreadsheetRangeId(string id)
        {
            string[] parts = id.Split(':');
            return parts.Length == 2;
        }

        // regex to find variables [var] inside an expression
        public static string VariableRegex = @"\[([^\]]*)\]";

        // regex to find spreadsheet range [s_x1_y1:s_x2:y2] inside an expression
        public static string RangeRegex = @"\[([^:]*):([^\]]*)\]";

        // regex to filter textbox with numeric input
        public static string NumericRegex = @"^-?\d*\.?\d*$";
    }
}
