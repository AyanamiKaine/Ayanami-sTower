

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Text;
using Wasmtime;

namespace wasmtime_explorations
{
    public class WasmtimeTests
    {
        [Fact]
        public void TestHelloWasm()
        {
            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, "wasm/hello.wat");
            using var linker = new Linker(engine);
            using var store = new Store(engine);

            var text = "";
            linker.Define("", "hello", Function.FromCallback(store, () => { text = "Hello from C#!"; }));

            var instance = linker.Instantiate(store, module);
            var run = instance.GetAction("run") ?? throw new InvalidOperationException("Failed to get run export");
            run();

            Assert.Equal("Hello from C#!", text);
        }

        [Fact]
        public void TestHelloReturnWasm()
        {
            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, "wasm/hello_return.wat");
            using var store = new Store(engine);
            var instance = new Instance(store, module, []);

            var getGreeting = instance.GetFunction<(int, int)>("get_greeting") ?? throw new InvalidOperationException("Failed to get get_greeting export");
            var (ptr, len) = getGreeting();

            var memory = instance.GetMemory("memory") ?? throw new InvalidOperationException("Failed to get memory export");
            var message = memory.ReadString(ptr, len, Encoding.UTF8);

            Assert.Equal("Hello from WASM!", message);
        }

        [Fact]
        public void TestHelloTwoWayWasm()
        {
            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, "wasm/hello_two_way.wat");
            using var store = new Store(engine);
            var instance = new Instance(store, module, []);

            var memory = instance.GetMemory("memory") ?? throw new InvalidOperationException("Failed to get memory export");
            var allocate = instance.GetFunction<int, int>("allocate") ?? throw new InvalidOperationException("Failed to get allocate export");
            var deallocate = instance.GetAction<int, int>("deallocate") ?? throw new InvalidOperationException("Failed to get deallocate export");
            var formatGreeting = instance.GetFunction<int, int, (int, int)>("format_greeting") ?? throw new InvalidOperationException("Failed to get format_greeting export");

            const string name = "Ayanami";
            var nameBytes = Encoding.UTF8.GetBytes(name);

            var namePtr = allocate(nameBytes.Length);
            var span = memory.GetSpan(namePtr, nameBytes.Length);
            nameBytes.CopyTo(span);

            var (resultPtr, resultLen) = formatGreeting(namePtr, nameBytes.Length);
            var result = memory.ReadString(resultPtr, resultLen, Encoding.UTF8);

            Assert.Equal($"Hello, {name}! Welcome to C#.", result);

            deallocate(namePtr, nameBytes.Length);
        }
    }
}
