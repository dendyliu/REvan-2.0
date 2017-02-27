namespace NCalc
{
    public delegate void EvaluateFunctionHandler(string name, FunctionArgs args);
    // return true if function exists and valid, false if function not found
    // should throw error with explanation if function exists but not valid
    public delegate bool ValidateFunctionHandler(string name, FunctionArgs args);
}
