using System;
namespace Avalonia.Flecs.StellaLearning.Data;

/// <summary>
/// Represents a tag
/// </summary>
public class Tag : IEquatable<Tag>
{
    /// <summary>
    /// Name of the tag
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Create a new tag
    /// </summary>
    /// <param name="name"></param>
    public Tag(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Check if the tag is equal to another tag
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(Tag? other)
    {
        if (other is null)
            return false;

        return Name == other.Name;
    }

    /// <summary>
    /// Check if the tag is equal to another object
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if (obj is Tag tag)
            return Equals(tag);

        return false;
    }
    /// <summary>
    /// Get the hash code of the tag
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    /// <summary>
    /// Get the string representation of the tag
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return Name;
    }
}