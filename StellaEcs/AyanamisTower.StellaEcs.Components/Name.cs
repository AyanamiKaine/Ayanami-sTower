namespace AyanamisTower.StellaEcs.Components;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Mostly used to give enties some unique name
/// </summary>
public struct Name(string value = "")
{
    public string Value = value;
}
