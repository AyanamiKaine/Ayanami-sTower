using System.Buffers;

namespace MemoryOfTTests;

public class Basics
{

    public class MyTestClass()
    {
        public int Value = 5;
    }

    /// <summary>
    /// So how do we create a Memory<T>? and how do we use it?
    /// </summary>
    [Fact]
    public void InitalCreation()
    {
        // Memory<T> is a struct that represents a contiguous region of arbitrary memory.
        // here it should be of type string, so we have a region of memory that consists only of type string
        Memory<string> memory = new string[] { "Hello, World!" };
        Assert.Equal("Hello, World!", memory.Span[0]);
    }


    // Now what if we want to add a string object to an memory segment after it was created?
    [Fact]
    public void AddingToAnAlreadyCreatedMemory()
    {
        // Again we are creating a region of memory of type string
        // Similar to how it needs to be done in C, we would need to create a new region make it bigger and copy the old region into it.
        Memory<string> memory = new string[] { "Hello, World!" };
        Memory<string> newString = new string[] { memory.Span[0], "Another string!" };

        newString.TryCopyTo(memory);
        Assert.Equal("Another string!", newString.Span[1]);
        // Now we want to add a new string to the memory region.
    }


    // How can we just use the underlying bytes created?
    [Fact]
    public void UsingTheByteTypeInMemory()
    {
        // We are allocating a region of 3000 bytes. 
        // and we can use them as we want.
        Memory<byte> memory = new byte[3000];

        memory.Span[0] = (byte)'H';
        memory.Span[1] = (byte)100;

        Assert.Equal('H', (char)memory.Span[0]);
        Assert.Equal(100, (int)memory.Span[1]);
    }

    // How can we allocate a class in a memory<T> ?
    [Fact]
    public void AllocatingClass()
    {
        MyTestClass[] testClasses = [
            new ()
        ];

        Memory<MyTestClass> memory = new(testClasses);
        Assert.Equal(5, memory.Span[0].Value);
    }
}
