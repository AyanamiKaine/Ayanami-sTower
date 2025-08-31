using System;
using AyanamisTower.StellaEcs.StellaInvicta.Assets;
using AyanamisTower.StellaEcs.StellaInvicta.Graphics;
using MoonWorks;
using MoonWorks.Graphics;
using StellaInvicta.Graphics;

namespace AyanamisTower.StellaEcs.StellaInvicta.Components;


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
    public Shader(Game game, string name)
    {
        Game = game;
        Name = name;
        RenderMode = new RenderMode();
        BuildShader();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Shader"/> class with a custom render mode.
    /// </summary>
    /// <param name="renderMode"></param>
    /// <param name="game"></param>
    /// <param name="name"></param>
    public Shader(Game game, string name, RenderMode renderMode)
    {
        Game = game;
        Name = name;
        RenderMode = renderMode;
        BuildShader();
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
