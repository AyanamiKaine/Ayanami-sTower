using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AyanamisTower.StellaEcs.StellaInvicta.Assets;
using AyanamisTower.StellaEcs.StellaInvicta.Graphics;
using MoonWorks;
using MoonWorks.Graphics;
using StellaInvicta.Graphics;

namespace AyanamisTower.StellaEcs.StellaInvicta.Components;


/*
TODO: We need to implement a way to cache compiled shaders and reuse them across entities. As well as
the created pipelines. We should not create the same shader for each entity that uses them as well as creating a pipeline for each entity with the same shader.
*/

/// <summary>
/// Represents a shader configuration for an entity.
/// This component allows entities to specify which shaders to use for rendering.
/// When you define a shader component for an entity it creates an appropriate render
/// pipeline for it. 
/// 
/// The default is         
/// Blend = BlendMode.NoBlend;
/// Cull = CullMode.CounterClockwiseBack;
/// Depth = DepthMode.DrawAlways;
/// Wireframe = WireframeMode.None;
/// </summary>
public class Shader
{
    /// <summary>
    /// Gets the game instance associated with this shader.
    /// </summary>
    public Game Game { get; }
    /// <summary>
    /// Gets the name of the shader.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Gets or sets the shader format. By default, this is set to HLSL.
    /// </summary>
    public ShaderCross.ShaderFormat Format { get; set; } = ShaderCross.ShaderFormat.HLSL;
    /// <summary>
    /// Gets or sets the rendering mode for the shader.
    /// </summary>
    public RenderMode RenderMode { get; set; } = new RenderMode();
    /// <summary>
    /// Called for every vertex the material is visible on.
    /// </summary>
    public MoonWorks.Graphics.Shader? VertexShader { get; set; }
    /// <summary>
    /// Called for every pixel the material is visible on. Often also called a pixel shader.
    /// </summary>
    public MoonWorks.Graphics.Shader? FragmentShader { get; set; }

    /// <summary>
    /// Cached graphics pipeline created for this shader.
    /// Call <see cref="BuildOrUpdatePipeline"/> to create or refresh this pipeline.
    /// </summary>
    public GraphicsPipeline? Pipeline { get; private set; }

    // Hot-reload support fields
    private FileSystemWatcher? fileWatcher;
    private Timer? debounceTimer;
    private readonly object buildLock = new object();
    /// <summary>
    /// When true, a FileSystemWatcher will watch the shader source file and attempt
    /// to rebuild the shaders when the file changes. On build failure the previous
    /// valid shaders are kept.
    /// </summary>
    public bool HotReloadEnabled { get; private set; } = false;

    /// <summary>
    /// Create a GraphicsPipeline for this shader using the provided <see cref="PipelineFactory"/> and vertex input layout.
    /// This keeps pipeline creation close to the shader component so entities that own this component can easily build
    /// a matching pipeline.
    /// </summary>
    /// <param name="factory">PipelineFactory configured for the current GraphicsDevice and swap/depth formats.</param>
    /// <param name="vertexInput">Vertex input state describing vertex attributes for the mesh to render.</param>
    /// <param name="pipelineName">Optional pipeline name override. Defaults to the shader name.</param>
    /// <returns>A created <see cref="GraphicsPipeline"/> instance.</returns>
    public GraphicsPipeline CreatePipeline(PipelineFactory factory, VertexInputState vertexInput, string? pipelineName = null)
    {
        ArgumentNullException.ThrowIfNull(factory);

        // Ensure shaders are built
        if (VertexShader == null || FragmentShader == null)
        {
            BuildShader();
        }

        if (VertexShader == null || FragmentShader == null)
        {
            throw new InvalidOperationException("Vertex and fragment shaders must be available to create a pipeline.");
        }

        var name = pipelineName ?? Name;

        var builder = factory.CreatePipeline(name + "Pipeline")
            .WithShaders(VertexShader, FragmentShader)
            .WithVertexInput(vertexInput);

        // Blend
        builder = RenderMode.Blend switch
        {
            BlendMode.NoBlend or BlendMode.Opaque => builder.WithBlendState(ColorTargetBlendState.NoBlend),
            BlendMode.NonPremultipliedAlpha => builder.WithBlendState(ColorTargetBlendState.NonPremultipliedAlphaBlend),
            BlendMode.PremultipliedAlpha => builder.WithBlendState(ColorTargetBlendState.PremultipliedAlphaBlend),
            BlendMode.Additive => builder.WithBlendState(ColorTargetBlendState.Additive),
            BlendMode.NoWrite => builder.WithBlendState(ColorTargetBlendState.NoWrite),// Map to the engine's NoWrite preset which disables color writes.
            _ => builder.WithBlendState(ColorTargetBlendState.NoBlend),
        };

        // Rasterizer: culling and wireframe
        RasterizerState raster = RasterizerState.CCW_CullBack;
        switch (RenderMode.Cull)
        {
            case Graphics.CullMode.None:
                raster = RasterizerState.CCW_CullNone;
                break;
            case Graphics.CullMode.CounterClockwiseBack:
                raster = RasterizerState.CCW_CullBack;
                break;
            case Graphics.CullMode.ClockwiseBack:
                raster = new RasterizerState { CullMode = MoonWorks.Graphics.CullMode.Back, FrontFace = FrontFace.Clockwise };
                break;
            case Graphics.CullMode.CounterClockwiseFront:
                raster = new RasterizerState { CullMode = MoonWorks.Graphics.CullMode.Front, FrontFace = FrontFace.CounterClockwise };
                break;
            case Graphics.CullMode.ClockwiseFront:
                raster = new RasterizerState { CullMode = MoonWorks.Graphics.CullMode.Front, FrontFace = FrontFace.Clockwise };
                break;
            case Graphics.CullMode.ClockwiseDisabled:
                raster = new RasterizerState { CullMode = MoonWorks.Graphics.CullMode.None, FrontFace = FrontFace.Clockwise };
                break;
            case Graphics.CullMode.CounterClockwiseDisabled:
                raster = new RasterizerState { CullMode = MoonWorks.Graphics.CullMode.None, FrontFace = FrontFace.CounterClockwise };
                break;
        }

        // Wireframe fill mode
        if (RenderMode.Wireframe != WireframeMode.None)
        {
            raster = new RasterizerState
            {
                CullMode = raster.CullMode,
                FrontFace = raster.FrontFace,
                FillMode = FillMode.Line
            };
        }

        builder = builder.WithRasterizer(raster);

        // Depth mode
        builder = RenderMode.Depth switch
        {
            DepthMode.DrawOpaque => builder.WithDepthTesting(true, true, CompareOp.LessOrEqual),
            DepthMode.DrawAlways => builder.WithDepthTesting(true, true, CompareOp.Always),// Always pass; still write depth so this object effectively forces its depth into the buffer.
            DepthMode.DrawNever => builder.WithDepthTesting(true, false, CompareOp.Never),
            DepthMode.PrepassAlpha => builder.WithDepthTesting(true, true, CompareOp.LessOrEqual),
            DepthMode.TestDisabled => builder.WithDepthTesting(false, false),
            _ => builder.WithDepthTesting(true, true, CompareOp.LessOrEqual),
        };
        return builder.Build();
    }

    /// <summary>
    /// Build or update the cached <see cref="Pipeline"/> using the provided factory and vertex input.
    /// Disposes any previously-created pipeline before assigning the new one.
    /// </summary>
    public MoonWorks.Graphics.GraphicsPipeline BuildOrUpdatePipeline(PipelineFactory factory, VertexInputState vertexInput, string? pipelineName = null)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var newPipeline = CreatePipeline(factory, vertexInput, pipelineName);

        // Dispose previous pipeline if present to avoid leaking GPU resources
        try
        {
            Pipeline?.Dispose();
        }
        catch
        {
            // swallow - disposal failures are non-fatal here; caller can log if desired
        }

        Pipeline = newPipeline;
        return Pipeline;
    }

    /// <summary>
    /// Represents a shader configuration for an entity.
    /// This component allows entities to specify which shaders to use for rendering.
    /// When you define a shader component for an entity it creates an appropriate render
    /// pipeline for it. 
    /// 
    /// The default is         
    /// Blend = BlendMode.NoBlend;
    /// Cull = CullMode.CounterClockwiseBack;
    /// Depth = DepthMode.DrawAlways;
    /// Wireframe = WireframeMode.None;
    /// </summary>
    public Shader(Game game, string name, bool enableHotReload = false)
    {
        Game = game;
        Name = name;
        RenderMode = new RenderMode();
        BuildShader();

        if (enableHotReload)
            EnableHotReload();
    }

    /// <summary>
    /// Enable or disable hot-reloading for this shader. When enabled a FileSystemWatcher will
    /// monitor the shader source file and attempt to rebuild it after edits. If the rebuild
    /// succeeds the shaders are swapped in and any existing pipeline is disposed (so the
    /// caller can recreate a matching pipeline). If the rebuild fails the previous shaders
    /// remain in use.
    /// </summary>
    public void EnableHotReload(bool enable = true)
    {
        if (enable == HotReloadEnabled) return;
        HotReloadEnabled = enable;
        if (enable)
            StartFileWatcher();
        else
            StopFileWatcher();
    }

    /// <summary>
    /// disable hot-reloading for this shader. When enabled a FileSystemWatcher will
    /// monitor the shader source file and attempt to rebuild it after edits. If the rebuild
    /// succeeds the shaders are swapped in and any existing pipeline is disposed (so the
    /// caller can recreate a matching pipeline). If the rebuild fails the previous shaders
    /// remain in use.
    /// </summary>
    public void DisableHotReload()
    {
        if (!HotReloadEnabled) return;
        HotReloadEnabled = false;
        StopFileWatcher();
    }

    private void StartFileWatcher()
    {
        // Compose the absolute path to the shader file on the developer filesystem
        // (TitleStorage is an abstract container; for watching we use the real file under AppContext.BaseDirectory)
        var shaderFile = Path.Combine(AppContext.BaseDirectory, AssetManager.AssetFolderName, Name + ".hlsl");
        var directory = Path.GetDirectoryName(shaderFile) ?? AppContext.BaseDirectory;
        var filter = Path.GetFileName(shaderFile) ?? (Name + ".hlsl");

        fileWatcher = new FileSystemWatcher(directory, filter)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        fileWatcher.Changed += OnShaderFileChanged;
        fileWatcher.Created += OnShaderFileChanged;
        fileWatcher.Renamed += OnShaderFileChanged;
        fileWatcher.Deleted += OnShaderFileChanged;

        // Debounce timer - not started yet
        debounceTimer = new Timer(_ =>
        {
            // Run the reload asynchronously so FileSystem events can finish
            _ = Task.Run(() => TryReloadShadersFromWatcher());
        }, null, Timeout.Infinite, Timeout.Infinite);
    }

    private void StopFileWatcher()
    {
        try
        {
            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Changed -= OnShaderFileChanged;
                fileWatcher.Created -= OnShaderFileChanged;
                fileWatcher.Renamed -= OnShaderFileChanged;
                fileWatcher.Deleted -= OnShaderFileChanged;
                fileWatcher.Dispose();
                fileWatcher = null;
            }

            debounceTimer?.Dispose();
            debounceTimer = null;
        }
        catch
        {
            // Swallow - best-effort cleanup
        }
    }

    private void OnShaderFileChanged(object? sender, FileSystemEventArgs e)
    {
        // Reset debounce (300ms)
        try
        {
            debounceTimer?.Change(300, Timeout.Infinite);
        }
        catch
        {
            // If timer hasn't been created for some reason, create and start one
            debounceTimer = new Timer(_ => { _ = Task.Run(() => TryReloadShadersFromWatcher()); }, null, 300, Timeout.Infinite);
        }
    }

    private void TryReloadShadersFromWatcher()
    {
        // Ensure only one rebuild at a time
        lock (buildLock)
        {
            try
            {
                // Attempt to build new shaders into temporaries
                if (TryBuildShaders(out var newVS, out var newFS))
                {
                    // Swap in the new shaders and dispose the old ones
                    var oldVS = VertexShader;
                    var oldFS = FragmentShader;

                    VertexShader = newVS;
                    FragmentShader = newFS;

                    try { oldVS?.Dispose(); } catch { }
                    try { oldFS?.Dispose(); } catch { }

                    // Existing pipelines using old shaders should be invalidated so callers can rebuild.
                    if (Pipeline != null)
                    {
                        try { Pipeline.Dispose(); } catch { }
                        Pipeline = null;
                    }

                    Console.WriteLine($"[Shader] Hot-reload succeeded for '{Name}'");
                }
                else
                {
                    Console.WriteLine($"[Shader] Hot-reload failed for '{Name}'; keeping previous valid shaders.");
                }
            }
            catch (Exception ex)
            {
                // Swallow - don't replace good shaders with a broken attempt
                Console.WriteLine($"[Shader] Exception during hot-reload for '{Name}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Attempts to build vertex and fragment shaders into out parameters without mutating the
    /// component's current shaders. Returns true only if both shaders were created successfully.
    /// On failure any partially-created shader objects are disposed.
    /// </summary>
    private bool TryBuildShaders(out MoonWorks.Graphics.Shader? newVertex, out MoonWorks.Graphics.Shader? newFragment)
    {
        newVertex = null;
        newFragment = null;
        try
        {
            newVertex = ShaderCross.Create(
                Game.GraphicsDevice,
                Game.RootTitleStorage,
                AssetManager.AssetFolderName + $"/{Name}.hlsl",
                "VSMain",
                Format,
                ShaderStage.Vertex,
                false,
                Name + "VS"
            );

            newFragment = ShaderCross.Create(
                Game.GraphicsDevice,
                Game.RootTitleStorage,
                AssetManager.AssetFolderName + $"/{Name}.hlsl",
                "PSMain",
                Format,
                ShaderStage.Fragment,
                false,
                Name + "FS"
            );

            if (newVertex == null || newFragment == null)
            {
                try { newVertex?.Dispose(); } catch { }
                try { newFragment?.Dispose(); } catch { }
                newVertex = null;
                newFragment = null;
                return false;
            }

            return true;
        }
        catch
        {
            try { newVertex?.Dispose(); } catch { }
            try { newFragment?.Dispose(); } catch { }
            newVertex = null;
            newFragment = null;
            return false;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Shader"/> class with a custom render mode.
    /// </summary>
    /// <param name="renderMode"></param>
    /// <param name="game"></param>
    /// <param name="name"></param>
    /// <param name="enableHotReload"></param>
    public Shader(Game game, string name, RenderMode renderMode, bool enableHotReload = false)
    {
        Game = game;
        Name = name;
        RenderMode = renderMode;
        BuildShader();

        if (enableHotReload)
            EnableHotReload();
    }

    /// <summary>
    /// Builds the shader for the entity.
    /// </summary>
    private void BuildShader()
    {
        VertexShader = ShaderCross.Create(
                Game.GraphicsDevice,
                Game.RootTitleStorage,
                AssetManager.AssetFolderName + $"/{Name}.hlsl",
                "VSMain",
                Format,
                ShaderStage.Vertex,
                false,
                Name + "VS"
            );

        FragmentShader = ShaderCross.Create(
            Game.GraphicsDevice,
            Game.RootTitleStorage,
            AssetManager.AssetFolderName + $"/{Name}.hlsl",
            "PSMain",
            Format,
            ShaderStage.Fragment,
            false,
            Name + "FS"
        );
    }
}
