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
        /*
        We have to run scripts that access the ECS world in the UI Thread otherwise
        functions related to flecs and the ECS world are defered until the function returns.

        For example saying entity.Set(new Component()) will set the component only AFTER
        the script has returned. This is because sharing the ECS world over different threads
        does not work. Or as far as I know it does not work. 
        The ECS World must exist in the same thread as the code that uses it.
        */
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var scriptManager = world.Get<ScriptManager>();
            await scriptManager.RunScriptAsync("ChangeWindowTitle");
        });
    }
}