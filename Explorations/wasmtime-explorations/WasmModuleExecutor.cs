using Wasmtime;

namespace wasmtime_explorations
{
    public class WasmModuleExecutor : IDisposable
    {
        private readonly Engine _engine;
        private readonly Module _module;

        public WasmModuleExecutor(string modulePath)
        {
            _engine = new Engine();
            _module = Module.FromTextFile(_engine, modulePath);
        }

        public Task<T> ExecuteInThread<T>(Func<Store, Instance, T> operation, Action<Linker, Store>? linkerSetup = null)
        {
            return Task.Run(() =>
            {
                using var linker = new Linker(_engine);
                using var store = new Store(_engine);

                linkerSetup?.Invoke(linker, store);

                var instance = linker.Instantiate(store, _module);
                return operation(store, instance);
            });
        }

        public void Dispose()
        {
            _module.Dispose();
            _engine.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
