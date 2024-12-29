using System.Runtime.InteropServices;

namespace ArenaAllocator;

public class Arena
{
    private Memory<byte> _arena;
    /// <summary>
    /// We use the offset to determine what bytes in the arena are not yet allocated
    /// Everything before the offset is allocated if the offset  + new allocated bytes is bigger
    /// then the Size of the arena it means that the arena is out of memory and needs to grow.
    /// </summary>
    private int _offset;
    public int Size => _arena.Length;

    /// <summary>
    /// By default, the arena will allocate 8MB of memory.
    /// </summary>
    public Arena()
    {
        _arena = new byte[MeasureOfInformation.EightMegabytes];
        _offset = 0;
    }

    public Arena(int size)
    {
        _arena = new byte[size];
        _offset = 0;
    }

    public Arena(Memory<byte> memory)
    {
        _arena = memory;
        _offset = 0;
    }

    /// <summary>
    /// By how much the arena should grow
    /// </summary>
    /// <param name="TimesBy"></param>
    public void Grow(byte bytesToGrowBy)
    {

    }

    /// <summary>
    /// Doubles the size of the arena
    /// </summary>
    public void DoubleSize()
    {

    }
}

internal static class MeasureOfInformation
{
    internal static int EightMegabytes => 8 * 1024 * 1024;
}
