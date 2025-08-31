using System;
using System.Numerics;
using AyanamisTower.StellaEcs.StellaInvicta.Components;

namespace AyanamisTower.StellaEcs.StellaInvicta.Graphics;

/// <summary>
/// A static class containing predefined material properties for various real-world objects.
/// These values are based on common standards used in computer graphics (like the OpenGL Red Book).
/// </summary>
public static class PredefinedMaterials
{
    /// <summary>
    /// Material properties for a shiny, deep red Ruby.
    /// </summary>
    public static readonly Material Ruby = new()
    {
        Ambient = new Vector3(0.1745f, 0.01175f, 0.01175f),
        Diffuse = new Vector3(0.61424f, 0.04136f, 0.04136f),
        Specular = new Vector3(0.727811f, 0.626959f, 0.626959f),
        Shininess = 76.8f
    };

    /// <summary>
    /// Material properties for Obsidian, a dark volcanic glass with sharp highlights.
    /// </summary>
    public static readonly Material Obsidian = new()
    {
        Ambient = new Vector3(0.05375f, 0.05f, 0.06625f),
        Diffuse = new Vector3(0.18275f, 0.17f, 0.22525f),
        Specular = new Vector3(0.332741f, 0.328634f, 0.346435f),
        Shininess = 38.4f
    };

    /// <summary>
    /// Material properties for a brilliant green Emerald.
    /// </summary>
    public static readonly Material Emerald = new()
    {
        Ambient = new Vector3(0.0215f, 0.1745f, 0.0215f),
        Diffuse = new Vector3(0.07568f, 0.61424f, 0.07568f),
        Specular = new Vector3(0.633f, 0.727811f, 0.633f),
        Shininess = 76.8f
    };

    /// <summary>
    /// Material properties for lustrous Gold.
    /// </summary>
    public static readonly Material Gold = new()
    {
        Ambient = new Vector3(0.24725f, 0.1995f, 0.0745f),
        Diffuse = new Vector3(0.75164f, 0.60648f, 0.22648f),
        Specular = new Vector3(0.628281f, 0.555802f, 0.366065f),
        Shininess = 51.2f
    };

    /// <summary>
    /// Material properties for polished Silver.
    /// </summary>
    public static readonly Material Silver = new()
    {
        Ambient = new Vector3(0.19225f, 0.19225f, 0.19225f),
        Diffuse = new Vector3(0.50754f, 0.50754f, 0.50754f),
        Specular = new Vector3(0.508273f, 0.508273f, 0.508273f),
        Shininess = 51.2f
    };

    /// <summary>
    /// Material properties for aged Bronze.
    /// </summary>
    public static readonly Material Bronze = new()
    {
        Ambient = new Vector3(0.2125f, 0.1275f, 0.054f),
        Diffuse = new Vector3(0.714f, 0.4284f, 0.18144f),
        Specular = new Vector3(0.393548f, 0.271906f, 0.166721f),
        Shininess = 25.6f
    };

    /// <summary>
    /// Material properties for a soft, iridescent Pearl.
    /// </summary>
    public static readonly Material Pearl = new()
    {
        Ambient = new Vector3(0.25f, 0.20725f, 0.20725f),
        Diffuse = new Vector3(1.0f, 0.829f, 0.829f),
        Specular = new Vector3(0.296648f, 0.296648f, 0.296648f),
        Shininess = 11.264f
    };

    /// <summary>
    /// Material properties for a matte black plastic.
    /// </summary>
    public static readonly Material BlackPlastic = new()
    {
        Ambient = new Vector3(0.0f, 0.0f, 0.0f),
        Diffuse = new Vector3(0.01f, 0.01f, 0.01f),
        Specular = new Vector3(0.50f, 0.50f, 0.50f),
        Shininess = 32.0f
    };

    /// <summary>
    /// Material properties for a dull green rubber.
    /// </summary>
    public static readonly Material GreenRubber = new()
    {
        Ambient = new Vector3(0.0f, 0.05f, 0.0f),
        Diffuse = new Vector3(0.4f, 0.5f, 0.4f),
        Specular = new Vector3(0.04f, 0.7f, 0.04f),
        Shininess = 10.0f
    };

    /// <summary>
    /// Material properties for Jade.
    /// </summary>
    public static readonly Material Jade = new()
    {
        Ambient = new Vector3(0.135f, 0.2225f, 0.1575f),
        Diffuse = new Vector3(0.54f, 0.89f, 0.63f),
        Specular = new Vector3(0.316228f, 0.316228f, 0.316228f),
        Shininess = 12.8f
    };

    /// <summary>
    /// Material properties for Turquoise.
    /// </summary>
    public static readonly Material Turquoise = new()
    {
        Ambient = new Vector3(0.1f, 0.18725f, 0.1745f),
        Diffuse = new Vector3(0.396f, 0.74151f, 0.69102f),
        Specular = new Vector3(0.297254f, 0.30829f, 0.306678f),
        Shininess = 12.8f
    };

    /// <summary>
    /// Material properties for Brass.
    /// </summary>
    public static readonly Material Brass = new()
    {
        Ambient = new Vector3(0.329412f, 0.223529f, 0.027451f),
        Diffuse = new Vector3(0.780392f, 0.568627f, 0.113725f),
        Specular = new Vector3(0.992157f, 0.941176f, 0.807843f),
        Shininess = 27.902f
    };

    /// <summary>
    /// Material properties for Chrome.
    /// </summary>
    public static readonly Material Chrome = new()
    {
        Ambient = new Vector3(0.25f, 0.25f, 0.25f),
        Diffuse = new Vector3(0.4f, 0.4f, 0.4f),
        Specular = new Vector3(0.774597f, 0.774597f, 0.774597f),
        Shininess = 76.8f
    };

    /// <summary>
    /// Material properties for Copper.
    /// </summary>
    public static readonly Material Copper = new()
    {
        Ambient = new Vector3(0.19125f, 0.0735f, 0.0225f),
        Diffuse = new Vector3(0.7038f, 0.27048f, 0.0828f),
        Specular = new Vector3(0.256777f, 0.137622f, 0.086014f),
        Shininess = 12.8f
    };

    /// <summary>
    /// Material properties for Cyan Plastic.
    /// </summary>
    public static readonly Material CyanPlastic = new()
    {
        Ambient = new Vector3(0.0f, 0.1f, 0.06f),
        Diffuse = new Vector3(0.0f, 0.50980392f, 0.50980392f),
        Specular = new Vector3(0.50196078f, 0.50196078f, 0.50196078f),
        Shininess = 32.0f
    };

    /// <summary>
    /// Material properties for Green Plastic.
    /// </summary>
    public static readonly Material GreenPlastic = new()
    {
        Ambient = new Vector3(0.0f, 0.0f, 0.0f),
        Diffuse = new Vector3(0.1f, 0.35f, 0.1f),
        Specular = new Vector3(0.45f, 0.55f, 0.45f),
        Shininess = 32.0f
    };

    /// <summary>
    /// Material properties for Red Plastic.
    /// </summary>
    public static readonly Material RedPlastic = new()
    {
        Ambient = new Vector3(0.0f, 0.0f, 0.0f),
        Diffuse = new Vector3(0.5f, 0.0f, 0.0f),
        Specular = new Vector3(0.7f, 0.6f, 0.6f),
        Shininess = 32.0f
    };

    /// <summary>
    /// Material properties for White Plastic.
    /// </summary>
    public static readonly Material WhitePlastic = new()
    {
        Ambient = new Vector3(0.0f, 0.0f, 0.0f),
        Diffuse = new Vector3(0.55f, 0.55f, 0.55f),
        Specular = new Vector3(0.70f, 0.70f, 0.70f),
        Shininess = 32.0f
    };

    /// <summary>
    /// Material properties for Yellow Plastic.
    /// </summary>
    public static readonly Material YellowPlastic = new()
    {
        Ambient = new Vector3(0.0f, 0.0f, 0.0f),
        Diffuse = new Vector3(0.5f, 0.5f, 0.0f),
        Specular = new Vector3(0.60f, 0.60f, 0.50f),
        Shininess = 32.0f
    };

    /// <summary>
    /// Material properties for Black Rubber.
    /// </summary>
    public static readonly Material BlackRubber = new()
    {
        Ambient = new Vector3(0.02f, 0.02f, 0.02f),
        Diffuse = new Vector3(0.01f, 0.01f, 0.01f),
        Specular = new Vector3(0.4f, 0.4f, 0.4f),
        Shininess = 10.0f
    };

    /// <summary>
    /// Material properties for Cyan Rubber.
    /// </summary>
    public static readonly Material CyanRubber = new()
    {
        Ambient = new Vector3(0.0f, 0.05f, 0.05f),
        Diffuse = new Vector3(0.4f, 0.5f, 0.5f),
        Specular = new Vector3(0.04f, 0.7f, 0.7f),
        Shininess = 10.0f
    };

    /// <summary>
    /// Material properties for Red Rubber.
    /// </summary>
    public static readonly Material RedRubber = new()
    {
        Ambient = new Vector3(0.05f, 0.0f, 0.0f),
        Diffuse = new Vector3(0.5f, 0.4f, 0.4f),
        Specular = new Vector3(0.7f, 0.04f, 0.04f),
        Shininess = 10.0f
    };

    /// <summary>
    /// Material properties for White Rubber.
    /// </summary>
    public static readonly Material WhiteRubber = new()
    {
        Ambient = new Vector3(0.05f, 0.05f, 0.05f),
        Diffuse = new Vector3(0.5f, 0.5f, 0.5f),
        Specular = new Vector3(0.7f, 0.7f, 0.7f),
        Shininess = 10.0f
    };

    /// <summary>
    /// Material properties for Yellow Rubber.
    /// </summary>
    public static readonly Material YellowRubber = new()
    {
        Ambient = new Vector3(0.05f, 0.05f, 0.0f),
        Diffuse = new Vector3(0.5f, 0.5f, 0.4f),
        Specular = new Vector3(0.7f, 0.7f, 0.04f),
        Shininess = 10.0f
    };
    /// <summary>
    /// Material properties for the default material.
    /// </summary>
    public static readonly Material Default = new()
    {
        Ambient = new Vector3(1.0f, 1.0f, 1.0f),
        Diffuse = new Vector3(1.0f, 1.0f, 1.0f),
        Specular = new Vector3(1.0f, 1.0f, 1.0f),
        Shininess = 0.0f
    };
}
