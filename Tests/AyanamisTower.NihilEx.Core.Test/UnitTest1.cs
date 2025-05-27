using System.Threading.Tasks;

namespace AyanamisTower.NihilEx.Test;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var core = new Core();
        await core.Run();
    }
}
