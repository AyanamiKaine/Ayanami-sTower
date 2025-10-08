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
        const string docText = "Squares a number. ( n -- n² )";
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

        Assert.Equal(docText, retrieved);
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

    [Fact]
    public void Example_ControlFlow_ConditionalExecution()
    {
        // Demonstrates Forth-style IF...ELSE...THEN control flow
        // Forth program: : BREAKFAST HURRIED? IF CEREAL ELSE EGGS THEN CLEAN ;
        var vm = new VMActor();

        // Define helper words that return values (mimicking Forth words that push to stack)
        vm.DefineNative("CEREAL", () => 1);   // Quick breakfast (returns 1)
        vm.DefineNative("EGGS", () => 2);     // Full breakfast (returns 2)
        vm.DefineNative("CLEAN", () => 99);   // Always clean up (returns 99)

        // Define BREAKFAST with conditional logic
        // In Forth: : BREAKFAST HURRIED? IF CEREAL ELSE EGGS THEN CLEAN ;
        // This demonstrates how IF...ELSE...THEN translates to our VM
        vm.DefineNative("BREAKFAST", (bool hurried) =>
        {
            // IF HURRIED? (parameter on stack)
            if (hurried)
            {
                // THEN CEREAL
                vm.ExecuteWord("CEREAL");
            }
            else
            {
                // ELSE EGGS
                vm.ExecuteWord("EGGS");
            }
            // THEN CLEAN (always executes after IF/ELSE)
            vm.ExecuteWord("CLEAN");
        });

        // Test hurried path: should execute CEREAL (1) then CLEAN (99)
        new ProgramBuilder().Push(1).Word("BREAKFAST").RunOn(vm);  // true = hurried
        Assert.Equal(2, vm.DataStack.Count());
        Assert.Equal(99, vm.DataStack.First().AsInteger());        // CLEAN on top
        Assert.Equal(1, vm.DataStack.Skip(1).First().AsInteger()); // CEREAL below

        // Test leisurely path: should execute EGGS (2) then CLEAN (99)
        // Create new VM to start with clean stack
        vm = new VMActor();
        vm.DefineNative("CEREAL", () => 1);
        vm.DefineNative("EGGS", () => 2);
        vm.DefineNative("CLEAN", () => 99);
        vm.DefineNative("BREAKFAST", (bool hurried) =>
        {
            if (hurried)
                vm.ExecuteWord("CEREAL");
            else
                vm.ExecuteWord("EGGS");
            vm.ExecuteWord("CLEAN");
        });

        new ProgramBuilder().Push(0).Word("BREAKFAST").RunOn(vm);  // false = not hurried
        Assert.Equal(2, vm.DataStack.Count());
        Assert.Equal(99, vm.DataStack.First().AsInteger());        // CLEAN on top
        Assert.Equal(2, vm.DataStack.Skip(1).First().AsInteger()); // EGGS below
    }

    [Fact]
    public void Example_ControlFlow_BytecodeJumps()
    {
        // Demonstrates Forth IF...ELSE...THEN using actual bytecode jumps
        // Forth program: : BREAKFAST HURRIED? IF CEREAL ELSE EGGS THEN CLEAN ;
        var vm = new VMActor();

        // Bytecode layout (byte offsets):
        // [0-2]:   JUMPZ      (1 opcode + 2 offset bytes = 3 total)
        // [3-11]:  PUSH 1     (1 opcode + 8 value bytes = 9 total) <- CEREAL  
        // [12-14]: JUMP       (1 opcode + 2 offset bytes = 3 total)
        // [15-23]: PUSH 2     (1 opcode + 8 value bytes = 9 total) <- EGGS
        // [24-32]: PUSH 99    (1 opcode + 8 value bytes = 9 total) <- CLEAN
        // [33]:    RETURN

        // When JUMPZ executes, IP will be at 3 (after reading opcode and offset)
        // To reach byte 15 (EGGS), offset = 15 - 3 = 12

        // When JUMP executes at byte 12, IP will be at 15 (after reading opcode and offset)
        // To reach byte 24 (CLEAN), offset = 24 - 15 = 9

        var breakfast = new BytecodeBuilder()
            .JumpZ(12)              // If false, jump to EGGS at byte 15 (offset from IP=3)
            .Push(1)                // CEREAL (bytes 3-11)
            .Jump(9)                // Jump to CLEAN at byte 24 (offset from IP=15)
            .Push(2)                // EGGS (bytes 15-23)
            .Push(99)               // CLEAN (bytes 24-32)
            .Op(OpCode.RETURN)
            .Build();

        vm.DefineWord("BREAKFAST", breakfast);

        // Test true path: CEREAL (1) then CLEAN (99)
        new ProgramBuilder().Push(1).Word("BREAKFAST").RunOn(vm);
        Assert.Equal(2, vm.DataStack.Count());
        Assert.Equal(99, vm.DataStack.First().AsInteger());        // CLEAN on top
        Assert.Equal(1, vm.DataStack.Skip(1).First().AsInteger()); // CEREAL below

        // Test false path: EGGS (2) then CLEAN (99)
        vm = new VMActor();
        vm.DefineWord("BREAKFAST", breakfast);
        new ProgramBuilder().Push(0).Word("BREAKFAST").RunOn(vm);
        Assert.Equal(2, vm.DataStack.Count());
        Assert.Equal(99, vm.DataStack.First().AsInteger());        // CLEAN on top
        Assert.Equal(2, vm.DataStack.Skip(1).First().AsInteger()); // EGGS below
    }

    [Fact]
    public void Example_ControlFlow_LoopWithJumps()
    {
        // Demonstrates a simple countdown loop using JUMPNZ (backward jump)
        // Pseudocode: counter = 3; do { counter--; } while (counter != 0);
        var vm = new VMActor();

        // Bytecode layout:
        // [0-8]:   PUSH 3      (9 bytes)
        // [9-17]:  PUSH 1      (9 bytes) <- loop_start
        // [18]:    SUB         (1 byte)
        // [19]:    DUP         (1 byte)
        // [20-22]: JUMPNZ      (3 bytes) - when this executes, IP will be at 23
        // [23]:    RETURN      (1 byte)
        //
        // To jump back to byte 9 from IP=23, offset = 9 - 23 = -14

        var countdown = new BytecodeBuilder()
            .Push(3)                // Start counter at 3 (bytes 0-8)
                                    // loop_start: (byte 9)
            .Push(1)                // Push 1 (bytes 9-17)
            .Op(OpCode.SUB)         // counter - 1 (byte 18)
            .Op(OpCode.DUP)         // Duplicate for test (byte 19)
            .JumpNZ(-14)            // Jump back to loop_start if non-zero (bytes 20-22)
            .Op(OpCode.RETURN)      // (byte 23)
            .Build();

        vm.DefineWord("COUNTDOWN", countdown);
        new ProgramBuilder().Word("COUNTDOWN").RunOn(vm);

        // Stack should have 0 on top (the final counter value after loop exits)
        Assert.Single(vm.DataStack);
        Assert.Equal(0, vm.DataStack.First().AsInteger());
    }

    [Fact]
    public void Example_ControlFlow_UsingIfElseThen()
    {
        // Demonstrates using IF/ELSE/THEN words to compile conditional logic
        // This is the Forth-style approach where control flow words generate bytecode
        var vm = new VMActor();

        // We'll compile the BREAKFAST word using IF/ELSE/THEN
        // Compiled code: DUP IF 1 ELSE 2 THEN 99 RETURN
        // Meaning: ( condition -- condition result 99 ) or ( condition -- condition result 99 )
        // Actually we want: ( condition -- result 99 )
        // So: IF 1 ELSE 2 THEN 99 RETURN

        int startAddr = vm.Here;

        // Compile bytecode using IF/ELSE/THEN
        // Note: IF/ELSE/THEN compile jump instructions, they don't execute
        // We need to emit opcodes manually for the PUSH instructions

        vm.ExecuteWord("IF");           // Compile JUMPZ (if condition is zero, skip to ELSE)

        // THEN branch: emit PUSH 1
        vm.PushValue(Value.Integer((byte)OpCode.PUSH));
        vm.ExecuteWord("EMIT");
        vm.PushValue(Value.Integer(1)); // CEREAL value
        vm.ExecuteWord("EMIT64");

        vm.ExecuteWord("ELSE");         // Compile JUMP to skip ELSE branch, backpatch IF

        // ELSE branch: emit PUSH 2
        vm.PushValue(Value.Integer((byte)OpCode.PUSH));
        vm.ExecuteWord("EMIT");
        vm.PushValue(Value.Integer(2)); // EGGS value
        vm.ExecuteWord("EMIT64");

        vm.ExecuteWord("THEN");         // Backpatch ELSE's JUMP target

        // After IF/ELSE/THEN: emit PUSH 99 (CLEAN)
        vm.PushValue(Value.Integer((byte)OpCode.PUSH));
        vm.ExecuteWord("EMIT");
        vm.PushValue(Value.Integer(99));
        vm.ExecuteWord("EMIT64");

        // Emit RETURN
        vm.PushValue(Value.Integer((byte)OpCode.RETURN));
        vm.ExecuteWord("EMIT");

        int endAddr = vm.Here;
        int codeLen = endAddr - startAddr;

        // Extract the compiled bytecode using reflection
        var dict = GetDictSpan(vm, startAddr, codeLen);
        var compiledCode = dict.ToArray();

        // Define BREAKFAST with the compiled code
        vm.DefineWord("BREAKFAST", compiledCode);

        // Test true path (non-zero condition): should get 1 (CEREAL) and 99 (CLEAN)
        new ProgramBuilder().Push(1).Word("BREAKFAST").RunOn(vm);
        Assert.Equal(2, vm.DataStack.Count());
        Assert.Equal(99, vm.DataStack.First().AsInteger());        // CLEAN on top
        Assert.Equal(1, vm.DataStack.Skip(1).First().AsInteger()); // CEREAL below

        // Test false path (zero condition): should get 2 (EGGS) and 99 (CLEAN)
        vm = new VMActor();
        vm.DefineWord("BREAKFAST", compiledCode);
        new ProgramBuilder().Push(0).Word("BREAKFAST").RunOn(vm);
        Assert.Equal(2, vm.DataStack.Count());
        Assert.Equal(99, vm.DataStack.First().AsInteger());        // CLEAN on top
        Assert.Equal(2, vm.DataStack.Skip(1).First().AsInteger()); // EGGS below
    }

    [Fact]
    public void Example_ControlFlow_ErgonomicIfElseThen()
    {
        // Demonstrates using ProgramBuilder's ergonomic If method
        // Much cleaner than manually calling ExecuteWord("IF"), EMIT, etc.
        var vm = new VMActor();

        int startAddr = vm.Here;

        // Compile: IF 1 ELSE 2 THEN 99 RETURN using ergonomic syntax
        new ProgramBuilder()
            .If(
                thenBranch => thenBranch.CompilePush(1),
                elseBranch => elseBranch.CompilePush(2)
            )
            .CompilePush(99)
            .CompileOp(OpCode.RETURN)
            .Execute(vm);

        int endAddr = vm.Here;
        int codeLen = endAddr - startAddr;

        // Extract the compiled bytecode
        var dict = GetDictSpan(vm, startAddr, codeLen);
        var compiledCode = dict.ToArray();

        // Define BREAKFAST with the compiled code
        vm.DefineWord("BREAKFAST", compiledCode);

        // Test true path (non-zero condition): should get 1 (CEREAL) and 99 (CLEAN)
        new ProgramBuilder().Push(1).Word("BREAKFAST").RunOn(vm);
        Assert.Equal(2, vm.DataStack.Count());
        Assert.Equal(99, vm.DataStack.First().AsInteger());        // CLEAN on top
        Assert.Equal(1, vm.DataStack.Skip(1).First().AsInteger()); // CEREAL below

        // Test false path (zero condition): should get 2 (EGGS) and 99 (CLEAN)
        vm = new VMActor();
        vm.DefineWord("BREAKFAST", compiledCode);
        new ProgramBuilder().Push(0).Word("BREAKFAST").RunOn(vm);
        Assert.Equal(2, vm.DataStack.Count());
        Assert.Equal(99, vm.DataStack.First().AsInteger());        // CLEAN on top
        Assert.Equal(2, vm.DataStack.Skip(1).First().AsInteger()); // EGGS below
    }

    private static Span<byte> GetDictSpan(VMActor vm, int addr, int len)
    {
        var fi = typeof(VMActor).GetField("_dictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var arr = (byte[])fi.GetValue(vm)!;
        return arr.AsSpan(addr, len);
    }
}
