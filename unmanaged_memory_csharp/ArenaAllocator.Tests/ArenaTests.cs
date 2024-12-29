namespace ArenaAllocator.Tests;

public class ArenaTests
{
    [Fact]
    public void CreateArena()
    {
        var arena = new Arena(); // The arena should come with a default size allocated for the heap, maybe 8mb?

        Assert.Equal(8 * 1024 * 1024, arena.Size);
    }

    [Fact]
    public void AllocatingAInt32()
    {
        var arena = new Arena();
        //int myInt = arena.Allocate<int>();
    }
}
