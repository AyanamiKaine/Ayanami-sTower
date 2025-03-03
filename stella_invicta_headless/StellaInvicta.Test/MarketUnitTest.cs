using Flecs.NET.Core;
using StellaInvicta.Tags.Identifiers;

namespace StellaInvicta.Test;



/// <summary>
/// Goods are sold and bought at markets. They provide a central point of exchange.
/// We always sell to a market (Imagine a floating space station or an entire planet) 
/// because of simplicity. But it should be possible to destroy or create markets.
/// to include or exclude someone etc.
/// 
/// A market is a place of coordination, a market has information for buying and selling orders
/// So for example companies create a selling order for gold at the price of 3000 credits for the
/// quantity of 2000. Now if someone buys it the goods can be transported from the seller to the buyer.
/// (The goods are not at the market) (The market is an institution that should facilitat trade between 
/// entities)
/// </summary>
public class MarketUnitTest
{
    [Fact]
    public void MarketBuyOrder()
    {
        World world = World.Create();
        world.Import<StellaInvictaECSModule>();

        var market = world.Entity("")
            .Add<Market>()
            .Set<BuyOrder, List<Entity>>([])
            .Set<SellOrder, List<Entity>>([]);

        var buyOrder = world.Entity("")
            .Add<BuyOrder>(market);

    }
}
