using AyanamisTower.StellaEcs.Api;
using AyanamisTower.StellaEcs.StellaInvicta.Components;
using AyanamisTower.StellaEcs.Components;
using System.Numerics;

namespace AyanamisTower.StellaEcs.StellaInvicta.Systems;

/// <summary>
/// System that collects and manages lighting data for the scene.
/// This system queries for all light components and prepares the data
/// to be passed to shaders for lighting calculations.
/// </summary>
public class LightingSystem : ISystem
{
    // Maximum number of lights supported (matches shader limits)
    private const int MaxDirectionalLights = 4;
    private const int MaxPointLights = 120;
    private const int MaxSpotLights = 16;

    /// <summary>
    /// The array of active directional lights.
    /// </summary>
    public DirectionalLight[] DirectionalLights { get; private set; } = new DirectionalLight[MaxDirectionalLights];
    /// <summary>
    /// The array of active point lights.
    /// </summary>
    public PointLight[] PointLights { get; private set; } = new PointLight[MaxPointLights];
    /// <summary>
    /// The array of active spot lights.
    /// </summary>
    public SpotLight[] SpotLights { get; private set; } = new SpotLight[MaxSpotLights];

    /// <summary>
    /// The positions of active point lights.
    /// </summary>
    public Vector3[] PointLightPositions { get; private set; } = new Vector3[MaxPointLights];
    /// <summary>
    /// The positions of active spot lights.
    /// </summary>
    public Vector3[] SpotLightPositions { get; private set; } = new Vector3[MaxSpotLights];

    /// <summary>
    /// The number of active directional lights.
    /// </summary>
    public int DirectionalLightCount { get; private set; }
    /// <summary>
    /// The number of active point lights.
    /// </summary>
    public int PointLightCount { get; private set; }
    /// <summary>
    /// The number of active spot lights.
    /// </summary>
    public int SpotLightCount { get; private set; }

    /// <inheritdoc/>
    public void Update(World world, float deltaTime)
    {
        // Reset counts
        DirectionalLightCount = 0;
        PointLightCount = 0;
        SpotLightCount = 0;

        // Collect directional lights
        foreach (var entity in world.Query(typeof(DirectionalLight)))
        {
            if (DirectionalLightCount >= MaxDirectionalLights) break;

            DirectionalLights[DirectionalLightCount] = entity.GetMut<DirectionalLight>();
            DirectionalLightCount++;
        }

        // Collect point lights
        foreach (var entity in world.Query(typeof(PointLight), typeof(Position3D)))
        {
            if (PointLightCount >= MaxPointLights) break;

            PointLights[PointLightCount] = entity.GetMut<PointLight>();
            PointLightPositions[PointLightCount] = entity.GetMut<Position3D>().Value;
            PointLightCount++;
        }

        // Collect spot lights
        foreach (var entity in world.Query(typeof(SpotLight), typeof(Position3D)))
        {
            if (SpotLightCount >= MaxSpotLights) break;

            SpotLights[SpotLightCount] = entity.GetMut<SpotLight>();
            SpotLightPositions[SpotLightCount] = entity.GetMut<Position3D>().Value;
            SpotLightCount++;
        }
    }

    /// <summary>
    /// Gets the total number of active lights.
    /// </summary>
    public int TotalLightCount => DirectionalLightCount + PointLightCount + SpotLightCount;

    /// <summary>
    /// Checks if there are any active lights in the scene.
    /// </summary>
    public bool HasLights => TotalLightCount > 0;

    /// <inheritdoc/>
    public string Name { get; set; } = "Lighting System";
    /// <inheritdoc/>
    public bool Enabled { get; set; } = true;
}
