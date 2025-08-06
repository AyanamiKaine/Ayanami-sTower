using System.Reflection;
using System.Runtime.Loader;

namespace AyanamisTower.StellaEcs;

/// <summary>
/// A custom AssemblyLoadContext that resolves dependencies from the plugin's own directory.
/// </summary>
public class PluginLoadContext(string pluginPath) : AssemblyLoadContext(isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new AssemblyDependencyResolver(pluginPath);

    // The Load method is called by the runtime when it needs to resolve an assembly name.
    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to resolve the assembly path using the .deps.json file.
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            // If the resolver finds the path, load the assembly into this context.
            Console.WriteLine($"[PluginLoadContext] Loading '{assemblyName.Name}' from stream.");
            using var fs = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read);
            return LoadFromStream(fs);
        }

        // If the resolver can't find it, we return null.
        // This allows the runtime to fall back to other contexts if needed,
        // though in our isolated model, it will likely result in an error, which is correct.
        return null;
    }

    /// <inheritdoc/>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
