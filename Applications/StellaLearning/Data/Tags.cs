/*
<one line to give the program's name and a brief idea of what it does.>
Copyright (C) <2025>  <Patrick, Grohs>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using System;
namespace AyanamisTower.StellaLearning.Data;

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