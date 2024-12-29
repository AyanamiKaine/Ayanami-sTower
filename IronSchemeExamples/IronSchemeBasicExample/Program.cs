using IronScheme;

// Define a Scheme function (within a string)
string schemeFunction = @"
(import (ironscheme clr))
";

// Evaluate the function definition
schemeFunction.Eval();



// Register the namespace with using-namespace
"""(clr-static-call System.Console WriteLine (clr-cast System.String "Hello World"))""".Eval();


