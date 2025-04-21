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
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Flecs.NET.Core;
using NLog;
using static Avalonia.Flecs.Controls.ECS.Module;

namespace AyanamisTower.StellaLearning.Pages;
/// <summary>
/// Home Page
/// </summary>
public class HomePage : IUIComponent
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    /// <summary>
    /// Creates a home page.
    /// </summary>
    /// <param name="world"></param>
    public HomePage(World world)
    {
        _root = world.UI<TextBlock>((t) => t.SetText("Home"))
            .Add<Page>().Entity;
    }

    /// <summary>
    /// Create a home page and attaches it to a parent
    /// </summary>
    /// <param name="world"></param>
    /// <param name="parent"></param>
    public HomePage(World world, Entity parent)
    {
        _root = world.UI<TextBlock>((t) => t.SetText("Home"))
            .Add<Page>().Entity
            .ChildOf(parent);
    }
}
