using System.Collections.Generic;
using System.Collections.ObjectModel;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia.Controls;
using Avalonia.Layout;
using NLog;
using Avalonia.Flecs.Controls;

namespace Avalonia.Flecs.StellaLearning.Pages;

/*
The main idea is to help me better study art. We need a way to 
add painting we want to be able to copy, see progessive 
(we need a way to associate done studies with our subjects)

Speed sketches, thumbnail sketches, should be part of our routine.
*/

/// <summary>
/// Art page, used for art studies,
/// like master copies,
/// collection of sketches,
/// </summary>
public class ArtPage : IUIComponent
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    /// <summary>
    /// Art page, used for art studies,
    /// like master copies,
    /// collection of sketches,
    /// </summary>
    public ArtPage(World world)
    {
        _root = world.UI<Grid>((grid) =>
        {

        }).Add<Page>().Entity;
    }
}