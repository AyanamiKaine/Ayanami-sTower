using Flecs.NET.Core;

namespace StellaInvicta.Test;



/// <summary>
/// Goods are sold and bought at markets. They provide a central point of exchange.
/// We always sell to a market (Imagine a floating space station or an entire planet) 
/// because of simplicity. But it should be possible to destroy or create markets.
/// to include or exclude someone etc.
/// </summary>
public class MarketUnitTest
{
    [Fact]
    public void MarketTest()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();
    }
}
