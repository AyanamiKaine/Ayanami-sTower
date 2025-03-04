using Flecs.NET.Core;
using StellaInvicta.Components;
using StellaInvicta.Tags.Identifiers;

namespace StellaInvicta.Test;

public class BuildingUnitTest
{

    /// <summary>
    /// Buildings are the corner storn of the econemy they produce various goods via input.
    /// </summary>
    [Fact]
    public void BuildingProduceOutput()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();

        var input = world.Entity("WoolClothingFactory-INPUT")
            // TODO: Maybe its maybe its better to say .IsA<Input>() 
            // Instead of saying .Add<Input>().
            .Add<Input>()
            .Set<Name>(new("Wool"))
            .Set<Quantity>(new(20));

        var output = world.Entity("ClothingClothingFactory-OUTPUT")
            .Add<Output>()
            .Set<Name>(new("Clothing"))
            .Set<Quantity>(new(5));



        var building = world.Entity("ClothingFactory-BUILDING")
            .Add<Building>()
            .Add<Inventory>()
            .Add<Input>(input)
            .Add<Output>(output);

        // TODO: before we can really work on the buildings we need to create a working underlying goods system.
        var buildingSimulation = () =>
        {
            building.Each<Input>((e) =>
            {
                // It would be really cool when we could write something like
                // This would ensure we always have the right amount of input
                // stored in the inventory
                /*
                if (inventory > input)
                    subtractInputFromInventory()
                    createOutput();
                */

            });

        };
        buildingSimulation();
        Assert.True(false);
    }
}
