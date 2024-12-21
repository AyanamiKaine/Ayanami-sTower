using Flecs.NET.Core;

using Avalonia.Flecs.Controls.ECS;
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia.Controls;
using Avalonia.Layout;
using System.Collections.ObjectModel;
using Avalonia.Flecs.Debug.Window.Data;
namespace Avalonia.Flecs.Debug.Window;

/// <summary>
/// Represents a window in the debug app.
/// </summary>
public class Window
{
    private World _world;
    private ObservableCollection<EntityDataRepresentation> _entities = [];
    /// <summary>
    /// The Debug windows shows various information about the ecs world.
    /// </summary>
    /// <param name="world"></param>
    public Window(World world)
    {
        _world = world;
        var debugWindow = world.Entity("DebugWindow")
            .Set(new Avalonia.Controls.Window())
            .SetWindowTitle("Avalonia.Flecs.Debug")
            .SetHeight(600)
            .SetWidth(800)
            .ShowWindow();

        var debugPage = world.Entity("DebugPage")
            .ChildOf(debugWindow)
            .Add<Page>()
            .Set(new Grid())
            .SetMargin(new Thickness(10))
            .SetColumnDefinitions("*, Auto, Auto")
            .SetRowDefinitions("Auto, *, Auto");

        var listSearchSpacedRepetition = world.Entity("ListSearchSpacedRepetition")
            .ChildOf(debugPage)
            .Set(new TextBox())
            .SetColumn(0)
            .SetWatermark("Search Entries");

        var totalEntities = world.Entity("totalEntities")
            .ChildOf(debugPage)
            .Set(new TextBlock())
            .SetVerticalAlignment(VerticalAlignment.Center)
            .SetMargin(new Thickness(10, 0))
            .SetText("Total Entities: 0")
            .SetColumn(1);

        Query q = world.QueryBuilder()
                      .With(Ecs.Any) // With any component/tag (wildcard)
                      .Build();

        ObservableCollection<EntityDataRepresentation> dummyItems = [];

        var scrollViewer = world.Entity("ScrollViewer")
            .ChildOf(debugPage)
            .Set(new ScrollViewer())
            .SetRow(1)
            .SetColumnSpan(1);


        var entitiesList = world.Entity("EntitiesList")
            .ChildOf(scrollViewer)
            .Set(new ListBox())
            .SetItemsSource(_entities)
            .SetSelectionMode(SelectionMode.Multiple);

        /*q.Each((Entity entity) =>
        {
            dummyItems.Add(new EntityDataRepresentation(entity));
            totalEntities.SetText($"Total Entities: {q.Count()}");
        });
        */
    }

    /// <summary>
    /// Adds a new entity to the debug window.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="name"></param>   
    public void AddEntity(Entity entity, string name)
    {
        _entities.Add(new EntityDataRepresentation(entity, name));
    }
}
