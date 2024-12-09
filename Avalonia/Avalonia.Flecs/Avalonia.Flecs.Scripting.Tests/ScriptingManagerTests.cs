
using Flecs.NET.Core;

namespace Avalonia.Flecs.Scripting.Tests;

public class ScriptingManagerTests
{
    [Fact]
    public void AddingScripts()
    {
        World world = World.Create();
        NamedEntities namedEntities = new(world);


        ScriptManager scriptManager = new(world, namedEntities, false);
        scriptManager.AddScript("TestScript", "using System;");

        Assert.True(scriptManager.CompiledScripts.Contains("TestScript"));
    }

    [Fact]
    public void OnScriptCompilationStart()
    {
        World world = World.Create();
        NamedEntities namedEntities = new(world);

        bool called = false;
        ScriptManager scriptManager = new(world, namedEntities, false);
        scriptManager.OnScriptCompilationStart += (sender, args) => called = true;
        scriptManager.AddScript("TestScript", "using System;");

        Assert.True(called);
    }

    [Fact]
    public void OnScriptCompilationFinished()
    {
        World world = World.Create();
        NamedEntities namedEntities = new(world);

        bool called = false;
        ScriptManager scriptManager = new(world, namedEntities, false);
        scriptManager.OnScriptCompilationFinished += (sender, args) => called = true;
        scriptManager.AddScript("TestScript", "using System;");

        Assert.True(called);
    }

    [Fact]
    public void OnCompiledScriptAdded()
    {
        World world = World.Create();
        NamedEntities namedEntities = new(world);

        bool called = false;
        ScriptManager scriptManager = new(world, namedEntities, false);
        scriptManager.OnCompiledScriptAdded += (sender, args) => called = true;
        scriptManager.AddScript("TestScript", "using System;");

        Assert.True(called);
    }

    [Fact]
    public void OnCompiledScriptChanged()
    {
        World world = World.Create();
        NamedEntities namedEntities = new(world);

        bool called = false;
        ScriptManager scriptManager = new(world, namedEntities, false);
        scriptManager.OnCompiledScriptChanged += (sender, args) => called = true;
        scriptManager.AddScript("TestScript", "using System;");
        scriptManager.AddScript("TestScript", "using System;");

        Assert.True(called);
    }

    [Fact]
    public void OnCompiledScriptRemoved()
    {
        World world = World.Create();
        NamedEntities namedEntities = new(world);

        bool called = false;
        ScriptManager scriptManager = new(world, namedEntities, false);
        scriptManager.OnCompiledScriptRemoved += (sender, args) => called = true;
        scriptManager.AddScript("TestScript", "using System;");
        scriptManager.RemoveScript("TestScript");

        Assert.True(called);
    }
}
