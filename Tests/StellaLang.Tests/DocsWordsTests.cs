namespace StellaLang.Tests;
#pragma warning disable CS1591

public class DocsWordsTests
{
    private static (int addr, int len) StoreUtf8(VMActor vm, string s)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        int addr = vm.Allocate(bytes.Length);
        for (int i = 0; i < bytes.Length; i++)
            vm.Write8(addr + i, bytes[i]);
        return (addr, bytes.Length);
    }

    private static Span<byte> UnsafeSpan(VMActor vm, int addr, int len)
    {
        // brute-force via reflection to get dictionary span for tests
        var fi = typeof(VMActor).GetField("_dictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var arr = (byte[])fi.GetValue(vm)!;
        return arr.AsSpan(addr, len);
    }

    [Fact]
    public void Docs_DefaultsExist_And_CanBeOverridden()
    {
        var vm = new VMActor();

        // Look up docs for ADD
        var (nameAddr, nameLen) = StoreUtf8(vm, "ADD");
        new ProgramBuilder().Push(nameAddr).Push(nameLen).Word("DOCS").RunOn(vm);
        // DOCS pushes (addr len) in this VM: we push addr, then len
        var docLen = (int)vm.DataStack.First().AsInteger();
        var docAddr = vm.DataStack.Skip(1).First().AsPointer();
        Assert.True(docLen > 0);

        var current = System.Text.Encoding.UTF8.GetString(UnsafeSpan(vm, docAddr, docLen));
        Assert.Contains("Add top two values", current);

        // Override docs for ADD
        var (newDocAddr, newDocLen) = StoreUtf8(vm, "Adds two numbers");
        new ProgramBuilder()
            .Push(nameAddr).Push(nameLen)
            .Push(newDocAddr).Push(newDocLen)
            .Word("DOCS!")
            .RunOn(vm);

        // Fetch again
        new ProgramBuilder().Push(nameAddr).Push(nameLen).Word("DOCS").RunOn(vm);
        var len2 = (int)vm.DataStack.First().AsInteger();
        var addr2 = vm.DataStack.Skip(1).First().AsPointer();
        var updated = System.Text.Encoding.UTF8.GetString(UnsafeSpan(vm, addr2, len2));
        Assert.Equal("Adds two numbers", updated);
    }

    [Fact]
    public void Docs_CustomWord_CanHaveDocs()
    {
        var vm = new VMActor();

        // Define a custom word FOO (push 42)
        vm.DefineWord("FOO", new BytecodeBuilder().Push(42).Op(OpCode.RETURN).Build());

        // Create name and doc in memory
        var (nAddr, nLen) = StoreUtf8(vm, "FOO");
        var (dAddr, dLen) = StoreUtf8(vm, "Pushes the answer");

        new ProgramBuilder().Push(nAddr).Push(nLen).Push(dAddr).Push(dLen).Word("DOCS!").RunOn(vm);

        new ProgramBuilder().Push(nAddr).Push(nLen).Word("DOCS").RunOn(vm);
        var len = (int)vm.DataStack.First().AsInteger();
        var addr = vm.DataStack.Skip(1).First().AsPointer();
        var txt = System.Text.Encoding.UTF8.GetString(UnsafeSpan(vm, addr, len));
        Assert.Equal("Pushes the answer", txt);
    }

    [Fact]
    public void Docs_ByWordHelpers_Work()
    {
        var vm = new VMActor();

        // Define BAR
        vm.DefineWord("BAR", new BytecodeBuilder().Push(7).Op(OpCode.RETURN).Build());

        // Set docs using DOCS!-W without name addr/len
        var (dAddr, dLen) = StoreUtf8(vm, "A bar word");

        // Debug: verify the bytes are written
        var testRead = System.Text.Encoding.UTF8.GetString(UnsafeSpan(vm, dAddr, dLen));
        Assert.Equal("A bar word", testRead);

        new ProgramBuilder().Push(dAddr).Push(dLen).Word("DOCS!-W").Word("BAR").RunOn(vm);

        // Lookup using DOCS-W without name addr/len
        new ProgramBuilder().Word("DOCS-W").Word("BAR").RunOn(vm);

        var len = (int)vm.DataStack.First().AsInteger();
        var addr = vm.DataStack.Skip(1).First().AsPointer();
        var txt = System.Text.Encoding.UTF8.GetString(UnsafeSpan(vm, addr, len));
        Assert.Equal("A bar word", txt);
    }
}
