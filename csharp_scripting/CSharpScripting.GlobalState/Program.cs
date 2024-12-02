using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;


//Adding Global State to a scripting environment


//Here we are creating a global state object that is based on 
//a read only dictionary. This object is then passed to the script
//you can then access the fields of the global state object in the script
//in this case the field is Data which is a dictionary that contains the
//Name and Age fields.
var globalData = new GlobalData(new Dictionary<string, object>
{
    { "Name", "Alice" },
    { "Age", 30 }
});


string code = @"
    Console.WriteLine($""Hello, {Data[""Name""]}!"");
";

var scriptOptions = ScriptOptions.Default
    .WithImports("System"); // Import the System namespace

// Create and execute the script
var script = CSharpScript.Create(code, scriptOptions, globalsType: typeof(GlobalData));
await script.RunAsync(globalData);

public class GlobalData(IReadOnlyDictionary<string, object> data)
{
    public IReadOnlyDictionary<string, object> Data { get; } = data;
}