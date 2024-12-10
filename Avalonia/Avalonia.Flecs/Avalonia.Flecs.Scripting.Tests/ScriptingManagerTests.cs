
using Avalonia.Controls;
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
    public async Task NamedEntitiesAreAvailableInScripts()
    {
        World world = World.Create();
        NamedEntities entities = new(world);
        var entity = entities["TestEntity"];

        ScriptManager scriptManager = new(world: world, entities: entities, recompileScriptsOnFileChange: false);
        scriptManager.AddScript("TestScript", "_entities[\"TestEntity\"].Set(new Button());");
        await scriptManager.RunScriptAsync("TestScript");
        Assert.True(entity.Has<Button>());
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

    /// <summary>
    /// We want to ensure that events are called in an expected order.
    /// </summary>
    [Fact]
    public void CorrectOrderOfEvents()
    {
        World world = World.Create();
        NamedEntities namedEntities = new(world);

        List<string> actualEvents = [];
        List<string> expectedEvents = ["Start", "Finished", "Added", "Start", "Finished", "Changed", "Added", "Removed"];

        ScriptManager scriptManager = new(world, namedEntities, false);
        scriptManager.OnScriptCompilationStart += (sender, args) => actualEvents.Add("Start");
        scriptManager.OnScriptCompilationFinished += (sender, args) => actualEvents.Add("Finished");
        scriptManager.OnCompiledScriptAdded += (sender, args) => actualEvents.Add("Added");
        scriptManager.OnCompiledScriptChanged += (sender, args) => actualEvents.Add("Changed");
        scriptManager.OnCompiledScriptRemoved += (sender, args) => actualEvents.Add("Removed");

        scriptManager.AddScript("TestScript", "using System;");
        scriptManager.AddScript("TestScript", "using System;");
        scriptManager.RemoveScript("TestScript");

        Assert.Equal(expectedEvents, actualEvents);
    }
}
