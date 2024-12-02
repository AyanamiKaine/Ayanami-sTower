using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

var scriptOptions = ScriptOptions.Default
    .AddReferences(typeof(System.Console).Assembly); // Import the System namespace

var code = File.ReadAllText("Scripts/script.cs");
// Create and execute the script
var script = CSharpScript.Create(code, null);
await script.RunAsync();