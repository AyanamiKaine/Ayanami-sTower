using System;

namespace StellaLang.Tests;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Showcases the power and flexibility of the VM through real-world examples.
/// </summary>
public class PowerExampleTests
{
    [Fact]
    public void Example_DynamicBehaviorViaRedefinition_StrategyPattern()
    {
        // Demonstrates runtime strategy switching without inheritance or interfaces
        var vm = new VMActor();

        // Define a generic DISCOUNT word (10% off)
        vm.DefineWord("DISCOUNT", new BytecodeBuilder()
            .Push(90)
            .Op(OpCode.MUL)
            .Push(100)
            .Op(OpCode.DIV)
            .Op(OpCode.RETURN)
            .Build());

        // Client code uses DISCOUNT
        new ProgramBuilder().Push(100).Word("DISCOUNT").RunOn(vm);
        Assert.Equal(90, vm.DataStack.First().AsInteger());

        // Black Friday: redefine DISCOUNT to 50% off
        vm.RedefineWord("DISCOUNT", new BytecodeBuilder()
            .Push(50)
            .Op(OpCode.MUL)
            .Push(100)
            .Op(OpCode.DIV)
            .Op(OpCode.RETURN)
            .Build());

        new ProgramBuilder().Push(100).Word("DISCOUNT").RunOn(vm);
        Assert.Equal(50, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void Example_InformationHiding_AbstractDataTypes()
    {
        // Classic Forth pattern: change implementation without changing client code
        var vm = new VMActor();

        // Phase 1: APPLES is a simple variable
        vm.ExecuteWord("VARIABLE");
        vm.ExecuteWord("APPLES");
        new ProgramBuilder().Push(10).Word("APPLES").Word("!").RunOn(vm);

        new ProgramBuilder().Word("APPLES").Word("@").RunOn(vm);
        Assert.Equal(10, vm.DataStack.First().AsInteger());

        // Phase 2: Requirements change - need RED and GREEN apples
        vm.ExecuteWord("VARIABLE");
        vm.ExecuteWord("COLOR");
        vm.ExecuteWord("VARIABLE");
        vm.ExecuteWord("REDS");
        vm.ExecuteWord("VARIABLE");
        vm.ExecuteWord("GREENS");

        // Get addresses for use in bytecode
        new ProgramBuilder().Word("REDS").RunOn(vm);
        var redsAddr = vm.DataStack.First().AsPointer();
        new ProgramBuilder().Word("GREENS").RunOn(vm);
        var greensAddr = vm.DataStack.First().AsPointer();
        new ProgramBuilder().Word("COLOR").RunOn(vm);
        var colorAddr = vm.DataStack.First().AsPointer();

        // Initialize COLOR to point to REDS
        new ProgramBuilder().Push(redsAddr).Push(colorAddr).Op(OpCode.STORE).RunOn(vm);

        // Define selector words using direct addresses
        vm.DefineWord("RED", new BytecodeBuilder()
            .Push(redsAddr)
            .Push(colorAddr)
            .Op(OpCode.STORE)
            .Op(OpCode.RETURN)
            .Build());

        vm.DefineWord("GREEN", new BytecodeBuilder()
            .Push(greensAddr)
            .Push(colorAddr)
            .Op(OpCode.STORE)
            .Op(OpCode.RETURN)
            .Build());

        // Redefine APPLES as an indirect variable (fetches what COLOR points to)
        vm.RedefineWord("APPLES", new BytecodeBuilder()
            .Push(colorAddr)
            .Op(OpCode.FETCH)
            .Op(OpCode.RETURN)
            .Build());

        // Client code UNCHANGED but now works with two types
        new ProgramBuilder().Word("RED").Push(20).Word("APPLES").Word("!").RunOn(vm);
        new ProgramBuilder().Word("GREEN").Push(5).Word("APPLES").Word("!").RunOn(vm);

        new ProgramBuilder().Word("RED").Word("APPLES").Word("@").RunOn(vm);
        Assert.Equal(20, vm.DataStack.First().AsInteger());

        new ProgramBuilder().Word("GREEN").Word("APPLES").Word("@").RunOn(vm);
        Assert.Equal(5, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void Example_CustomDataStructures_Circle()
    {
        // Memory-based structs with field accessors defined in VM
        var vm = new VMActor();

        StructDefiner.DefineI64Struct(vm, "CIRCLE",
            [
                new StructDefiner.Field("X", 0),
                new StructDefiner.Field("Y", 8),
                new StructDefiner.Field("RADIUS", 16)
            ],
            sizeBytes: 24);

        // Create circle at (5, 10) with radius 3
        new ProgramBuilder().Push(5).Push(10).Push(3).Word("NEW-CIRCLE").RunOn(vm);
        var addr = vm.DataStack.First().AsPointer();

        // Read fields
        new ProgramBuilder().Push(addr).Word("GET-CIRCLE-X").RunOn(vm);
        Assert.Equal(5, vm.DataStack.First().AsInteger());

        // Define utility word AREA using native definition (easier than bytecode)
        vm.DefineNative("CIRCLE-AREA", (Action<int>)(addr =>
        {
            vm.PushValue(Value.Pointer(addr));
            vm.ExecuteWord("GET-CIRCLE-RADIUS");
            var r = vm.PopValue().AsInteger();
            vm.PushValue(Value.Integer(3 * r * r));
        }));

        new ProgramBuilder().Push(addr).Word("CIRCLE-AREA").RunOn(vm);
        Assert.Equal(27, vm.DataStack.First().AsInteger());  // 3 * 3²
    }

    [Fact]
    public void Example_NativeInterop_MathLibrary()
    {
        // Direct access to .NET without wrappers
        var vm = new VMActor();

        vm.DefineNativeStatic("MAX", typeof(Math), nameof(Math.Max), typeof(int), typeof(int));
        vm.DefineNativeStatic("MIN", typeof(Math), nameof(Math.Min), typeof(int), typeof(int));
        vm.DefineNativeStatic("ABS", typeof(Math), nameof(Math.Abs), typeof(int));

        // Clamp function: using native words directly
        vm.DefineNative("CLAMP", (Action)(() =>
        {
            var upper = (int)vm.PopValue().AsInteger();
            var lower = (int)vm.PopValue().AsInteger();
            var value = (int)vm.PopValue().AsInteger();
            var clamped = Math.Min(Math.Max(value, lower), upper);
            vm.PushValue(Value.Integer(clamped));
        }));

        new ProgramBuilder().Push(150).Push(0).Push(100).Word("CLAMP").RunOn(vm);
        Assert.Equal(100, vm.DataStack.First().AsInteger());

        new ProgramBuilder().Push(-10).Push(0).Push(100).Word("CLAMP").RunOn(vm);
        Assert.Equal(0, vm.DataStack.First().AsInteger());

        new ProgramBuilder().Push(50).Push(0).Push(100).Word("CLAMP").RunOn(vm);
        Assert.Equal(50, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void Example_SelfDocumenting_BuiltInDocsSystem()
    {
        // VM can introspect and document itself
        var vm = new VMActor();

        // Define a custom word with documentation
        vm.DefineWord("SQUARE", new BytecodeBuilder()
            .Op(OpCode.DUP)
            .Op(OpCode.MUL)
            .Op(OpCode.RETURN)
            .Build());

        // Set docs using the by-word helper - allocate doc string first
        var docText = "Squares a number. ( n -- n² )";
        var docBytes = System.Text.Encoding.UTF8.GetBytes(docText);

        // Allocate space and write the doc bytes
        var docAddr = vm.Allocate(docBytes.Length);
        for (int i = 0; i < docBytes.Length; i++)
            vm.Write8(docAddr + i, docBytes[i]);

        new ProgramBuilder().Push(docAddr).Push(docBytes.Length).Word("DOCS!-W").Word("SQUARE").RunOn(vm);

        // Retrieve and verify
        new ProgramBuilder().Word("DOCS-W").Word("SQUARE").RunOn(vm);
        var len = (int)vm.DataStack.First().AsInteger();
        var addr = vm.DataStack.Skip(1).First().AsPointer();
        var retrieved = System.Text.Encoding.UTF8.GetString(GetDictSpan(vm, addr, len));

        Assert.Contains("Squares a number", retrieved);
    }

    [Fact]
    public void Example_ComposableWords_BuildingComplexBehavior()
    {
        // Small words compose into complex behavior
        var vm = new VMActor();

        // Base utilities
        vm.DefineWord("SQUARE", new BytecodeBuilder()
            .Op(OpCode.DUP).Op(OpCode.MUL).Op(OpCode.RETURN).Build());

        // Use native to avoid word calls in bytecode
        vm.DefineNative("CUBE", (Action)(() =>
        {
            var n = vm.PopValue().AsInteger();
            vm.PushValue(Value.Integer(n * n * n));
        }));

        // Pythagorean check: a²+b²=c²?
        vm.DefineNative("IS-PYTHAGOREAN", (Action)(() =>
        {
            var c = (int)vm.PopValue().AsInteger();
            var b = (int)vm.PopValue().AsInteger();
            var a = (int)vm.PopValue().AsInteger();
            var result = (a * a + b * b) - (c * c);
            vm.PushValue(Value.Integer(result));
        }));

        // Test 3-4-5 triangle
        new ProgramBuilder().Push(3).Push(4).Push(5).Word("IS-PYTHAGOREAN").RunOn(vm);
        Assert.Equal(0, vm.DataStack.First().AsInteger());  // 0 = equal

        // Test non-pythagorean
        new ProgramBuilder().Push(2).Push(3).Push(4).Word("IS-PYTHAGOREAN").RunOn(vm);
        Assert.NotEqual(0, vm.DataStack.First().AsInteger());  // non-zero = not equal
    }

    [Fact]
    public void Example_RuntimePolymorphism_ShapeArea()
    {
        // Different "shapes" with polymorphic AREA via redefinition
        var vm = new VMActor();

        // Define shape selector
        vm.ExecuteWord("VARIABLE");
        vm.ExecuteWord("SHAPE-TYPE");

        // Generic AREA word (starts as circle)
        vm.DefineWord("AREA", new BytecodeBuilder()
            .Op(OpCode.DUP).Op(OpCode.MUL).Push(3).Op(OpCode.MUL)  // 3*r²
            .Op(OpCode.RETURN)
            .Build());

        new ProgramBuilder().Push(5).Word("AREA").RunOn(vm);
        Assert.Equal(75, vm.DataStack.First().AsInteger());

        // Switch to square area
        vm.RedefineWord("AREA", new BytecodeBuilder()
            .Op(OpCode.DUP).Op(OpCode.MUL)  // side²
            .Op(OpCode.RETURN)
            .Build());

        new ProgramBuilder().Push(5).Word("AREA").RunOn(vm);
        Assert.Equal(25, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void Example_MemoryManagement_ManualAllocation()
    {
        // Direct memory control like C, but safe
        var vm = new VMActor();

        // Allocate array of 10 integers (80 bytes)
        int arrayBase = vm.Allocate(80);

        // Fill with squares: 0², 1², 2², ...
        for (int i = 0; i < 10; i++)
        {
            vm.Write64(arrayBase + i * 8, i * i);
        }

        // Read back and verify
        var values = new long[10];
        for (int i = 0; i < 10; i++)
        {
            values[i] = vm.Read64(arrayBase + i * 8);
        }

        Assert.Equal(0, values[0]);
        Assert.Equal(1, values[1]);
        Assert.Equal(4, values[2]);
        Assert.Equal(81, values[9]);
    }

    [Fact]
    public void Example_MixingBytecodeAndWords_SimplifiedDemo()
    {
        // Demonstrates mixing raw bytecode with higher-level words
        var vm = new VMActor();

        vm.ExecuteWord("VARIABLE");
        vm.ExecuteWord("COUNTER");

        // INCREMENT: ( -- )  counter @ 1 + counter !
        vm.DefineNative("INCREMENT", (Action)(() =>
        {
            new ProgramBuilder().Word("COUNTER").Word("@").RunOn(vm);
            var val = vm.PopValue().AsInteger();
            new ProgramBuilder().Push(val + 1).Word("COUNTER").Word("!").RunOn(vm);
        }));

        // Initialize to 0
        new ProgramBuilder().Push(0).Word("COUNTER").Word("!").RunOn(vm);

        // Call INCREMENT 5 times
        for (int i = 0; i < 5; i++)
            vm.ExecuteWord("INCREMENT");

        new ProgramBuilder().Word("COUNTER").Word("@").RunOn(vm);
        Assert.Equal(5, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void Example_StackManipulation_ReversePolishCalculator()
    {
        // Classic RPN calculator in action
        var vm = new VMActor();

        // Expression: (3 + 4) * (5 - 2) = 7 * 3 = 21
        new ProgramBuilder()
            .Push(3)
            .Push(4)
            .Op(OpCode.ADD)     // 7
            .Push(5)
            .Push(2)
            .Op(OpCode.SUB)     // 7 3
            .Op(OpCode.MUL)     // 21
            .RunOn(vm);

        Assert.Single(vm.DataStack);
        Assert.Equal(21, vm.DataStack.First().AsInteger());
    }

    private static Span<byte> GetDictSpan(VMActor vm, int addr, int len)
    {
        var fi = typeof(VMActor).GetField("_dictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var arr = (byte[])fi.GetValue(vm)!;
        return arr.AsSpan(addr, len);
    }
}
