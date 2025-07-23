
using System.Text;
using System.Threading.Tasks;
using Wasmtime;

namespace wasmtime_explorations
{
    public class WasmModuleExecutorTests
    {
        [Fact]
        public async Task TestHelloWasmWithExecutor()
        {
            using var executor = new WasmModuleExecutor("wasm/hello.wat");

            var text = "";
            void linkerSetup(Linker linker, Store store)
            {
                linker.Define("", "hello", Function.FromCallback(store, () => { text = "Hello from C#!"; }));
            }

            await executor.ExecuteInThread((store, instance) =>
            {
                var run = instance.GetAction("run") ?? throw new InvalidOperationException("Failed to get run export");
                run();
                return Task.CompletedTask;
            }, linkerSetup);

            Assert.Equal("Hello from C#!", text);
        }

        [Fact]
        public async Task TestConcurrentHelloWasmWithExecutor()
        {
            using var executor = new WasmModuleExecutor("wasm/hello.wat");

            var t1Result = "";
            void linkerSetup1(Linker linker, Store store)
            {
                linker.Define("", "hello", Function.FromCallback(store, () => { t1Result = "Hello from C# thread 1!"; }));
            }
            var t1 = executor.ExecuteInThread((store, instance) =>
            {
                var run = instance.GetAction("run") ?? throw new InvalidOperationException("Failed to get run export");
                run();
                return t1Result;
            }, linkerSetup1);

            var t2Result = "";
            void linkerSetup2(Linker linker, Store store)
            {
                linker.Define("", "hello", Function.FromCallback(store, () => { t2Result = "Hello from C# thread 2!"; }));
            }
            var t2 = executor.ExecuteInThread((store, instance) =>
            {
                var run = instance.GetAction("run") ?? throw new InvalidOperationException("Failed to get run export");
                run();
                return t2Result;
            }, linkerSetup2);

            var results = await Task.WhenAll(t1, t2);

            Assert.Equal("Hello from C# thread 1!", results[0]);
            Assert.Equal("Hello from C# thread 2!", results[1]);
        }
    }
}
