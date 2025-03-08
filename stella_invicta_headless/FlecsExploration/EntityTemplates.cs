using Flecs.NET.Core;

namespace FlecsExploration;

file record struct Name(string Value = "DefaultName");
file record struct Age(int Amount = 0);

file record struct Credits(double Amount = 0);
file struct Character;

/// <summary>
/// Sometimes we want to create templates for entities,
/// with a specific set of predefined components.
/// We do this by using an entity prefab.
/// 
/// Imagine IsA would be called TypeOf. Using the concept
/// we can compose and decompose types.
/// </summary>
public class EntityTemplates
{

    [Fact]
    public void CreatingATemplate()
    {
        World world = World.Create();

        // Here we define a character template
        var characterPrefab = world.Prefab("Character")
            .Add<Character>()
            .Set<Credits>(new(0))
            .Set<Age>(new(0))
            .Set<Name>(new("DEFAULT_NAME"));

        var melina = world.Entity("Melina")
            .IsA(characterPrefab)
            .Set<Name>(new("Melina"));

        Assert.True(
            melina.Has<Age>() &&
            melina.Has<Name>() &&
            melina.Has<Character>());
    }

    /// <summary>
    /// Its possible to associate a prefab also with a type.
    /// So instead of saying .IsA(prefabEntity) we could say
    /// .IsA TYPE ()
    /// </summary>
    [Fact]
    public void DefiningAPrefabAsAType()
    {
        World world = World.Create();

        // Here we define a character template
        var characterPrefab = world.Prefab<Character>("CharacterPrefab")
            .Add<Character>()
            .Set<Credits>(new(0))
            .Set<Age>(new(0))
            .Set<Name>(new("DEFAULT_NAME"));

        var melina = world.Entity("Melina")
            .IsA<Character>()
            .Set<Age>(new(20))
            .Set<Name>(new("Melina"));

        var luna = world.Entity("Luna")
            .IsA<Character>()
            .Set<Name>(new("Luna"));

        Assert.True(
            melina.Has<Age>() &&
            melina.Has<Name>() &&
            melina.Has<Character>() &&
            luna.Has<Age>() &&
            luna.Has<Name>() &&
            luna.Has<Character>());
    }
}