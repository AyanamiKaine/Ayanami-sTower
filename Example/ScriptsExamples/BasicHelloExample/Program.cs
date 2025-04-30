using CSScriptLib;

/// <summary>
/// Examples interface for a script to implement
/// </summary>
public interface ICalc
{
    /// <summary>
    /// Adds two numbers
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    int Add(int a, int b);
}

internal static class Program
{
    private static void Main()
    {
        CSScript.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom;

        ICalc script = (ICalc)
            CSScript.Evaluator.LoadCode(
                @"using System;
                                          public class Script : ICalc
                                          {
                                              public int Add(int a, int b)
                                              {
                                                  return a + b;
                                              }
                                          }"
            );
        int result = script.Add(1, 2);

        Console.WriteLine(result);
    }
}
