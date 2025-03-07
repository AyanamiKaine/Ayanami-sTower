using Flecs.NET.Core;

namespace FlecsExploration;


/// <summary>
/// Components are data containers the hold data we want to accociate with an entity.
/// The best way thinking about is the following. If you would have defined a field for a
/// game object instead turn it into a component. Every field normally attached to an object
/// can be turned into a component. There are no limitations. 
/// </summary>

public class Components
{


    public record struct Position2D(double X, double Y);
    public record struct Velocity2D(double X, double Y);

    /// <summary>
    /// There are various ways to create components in Flecs.NET.
    /// </summary>
    [Fact]
    public void CreatingComponents()
    {
        World world = World.Create();
        var entity = world.Entity();

        world.Component<Position2D>(); // This simply registers a component for a world and makes 
                                       // it available for entities to use.    

        entity
            .Set(new Position2D(0, 0)) // .Set adds and sets a value for a component.
            .Set(new Velocity2D(1, 0));// You can also use component that are not first registered
                                       // with the world. This will register the component for you. 

        var hasVelocity = entity.Has<Velocity2D>(); // Check if entity has a component
        var HasPosition = entity.Has<Position2D>(); // Check if entity has a component

        Assert.True(hasVelocity);
        Assert.True(HasPosition);
    }

    /// <summary>
    /// If there are so many ways a component can be registered in the world
    /// why do it manually?
    /// </summary>
    [Fact]
    public void RegisterComponentsInTheWorld()
    {
        World world = World.Create();
        var entity = world.Entity();

        /*
        We can define the memeber values of a component when we register it.
        This is used when we serialize and deserialize components.

        Also this makes it possible to set values in the web ui. 
        SEE REST.cs for more on that.

        It is always useful to define the member types of a component.
        */
        world.Component<Position2D>()
            .Member<double>("X")
            .Member<double>("Y");

        world.Component<Velocity2D>()
            .Member<double>("X")
            .Member<double>("Y");

        entity
            .Set(new Position2D(0, 0))
            .Set(new Velocity2D(1, 0));

        var json = entity.ToJson();
        Assert.NotNull(json);
    }


    /// <summary>
    /// Dont think that components can only be simple data like speed, position, name.
    /// Every object you could define is a valid component. Sometimes its better to define 
    /// a bigger object. Most of the time you should prefer using 3 component to replace one 
    /// big one. If you know relation databases and normalization think on those lines when 
    /// breaking down complex relationship between fields to turn them into seperate components.
    /// </summary>
    [Fact]
    public void CreatingComplexComponents()
    {

    }

    /// <summary>
    /// Sometimes we want to ensure an entity has a component attached to it,
    /// when we say Get component. Imagine using first has component and then get component,
    /// we can bundle this using e.Ensure component
    /// </summary>
    [Fact]
    public void EnsuringAnEntityHasAComponent()
    {
        World world = World.Create();
        var entity = world.Entity();

        // Here we get a mutable refrence, should the component not be a part of the entity it will be
        // first created and then returned.
        entity.Ensure<Position2D>().X = 2;


        Assert.True(entity.Has<Position2D>());
        // Using ensure again when the component exists simply returns a mut ref.
        Assert.Equal(2, entity.Ensure<Position2D>().X);
    }

    [Fact]
    public void EnsuringAnEntityHasAComponentReadonly()
    {
        World world = World.Create();
        var entity = world.Entity();

        entity.Ensure<Position2D>().X = 2;

        // Even though we return a mut ref by default does not mean we cannot create a immutable refrence
        ref readonly var positionOfEntity = ref entity.Ensure<Position2D>();

        Assert.True(entity.Has<Position2D>());
        // Using ensure again when the component exists simply returns a mut ref.
        Assert.Equal(2, positionOfEntity.X);
    }

    [Fact]
    public void ReadingComponentData()
    {
        World world = World.Create();
        var entity = world.Entity();

        world.Component<Position2D>()
            .Member<double>("X")
            .Member<double>("Y");

        entity
            .Set(new Position2D(5, 0));

        // In Flecs for C# we are using a ref and mark it as
        // readonly to indicate that we are not going to change the value.
        // In C++ we would get a pointer with get to the component data.
        // and would have to mark it as const.
        // Even though we are doing conceptually the same in C# the 
        // semantics are different.
        ref readonly var position = ref entity.Get<Position2D>();

        // Summary:
        // When you simply want to read the data of a component
        // use ref readonly to indicate that you are not going to change the value.
        // and ref Get<Component>

        Assert.Equal(5, position.X);
        Assert.Equal(0, position.Y);
    }

}