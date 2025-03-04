using Flecs.NET.Core;

namespace StellaInvicta.Test;

/// <summary>
/// Here we want to test the simulation for one province
/// The basic idea, is that a province has infrastructure,
/// buildings and population. Populations work at buildings
/// and buildings need workers, enough infrastructure and
/// their input goods to produce output. Buildings store the output
/// in their inventory and put them up to sale if wished. Buyers
/// come to the building and take the output and bring it to somewhere 
//  else
/// </summary>
public class ProvinceUnitTest
{
    [Fact]
    public void DefineAProvince()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();

        Assert.True(false);
    }
}
