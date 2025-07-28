#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using AyanamisTower.Explorations;

namespace Sparsed.Test;

/// <summary>
/// Unit tests for the SparsedSet functionality.
/// </summary>
public class SparsedSetUnitTest
{
    [Fact]
    public void InsertingTest()
    {
        var sparsedSet = new SparsedSet(8, 10000)
        {
            5,
            1,
            4
        };

        Assert.True(sparsedSet.Has(5));
        Assert.False(sparsedSet.Has(3));
    }
}
