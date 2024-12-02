using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

string code = @"
    Console.WriteLine(""Hello from the script!"");
";

var scriptOptions = ScriptOptions.Default
    .WithImports("System"); // Import the System namespace

// Create and execute the script
var script = CSharpScript.Create(code, scriptOptions);
await script.RunAsync();