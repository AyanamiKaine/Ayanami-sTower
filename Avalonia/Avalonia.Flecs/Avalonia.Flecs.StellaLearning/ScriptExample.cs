using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Flecs.NET.Core;
using System.Threading.Tasks;
using System.Reflection;
using Avalonia.Flecs.Scripting;
public static class ScriptExample
{
    public class GlobalData(World _world)
    {
        public World world = _world;
    }

    public static void AddScript()
    {

    }

    public static async Task RunScriptAsync(World world)
    {
        var scriptManager = world.Get<ScriptManager>();
        await scriptManager.RunScriptAsync("ChangeWindowTitle");
    }
}