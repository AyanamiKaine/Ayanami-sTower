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
            .SetColumnSpan(3);


        var entitiesList = world.Entity("EntitiesList")
            .ChildOf(scrollViewer)
            .Set(new ListBox())
            .SetItemsSource(dummyItems)
            .SetSelectionMode(SelectionMode.Multiple);

        q.Each((Entity entity) =>
        {
            dummyItems.Add(new EntityDataRepresentation(entity));
            totalEntities.SetText($"Total Entities: {q.Count()}");
        });
    }

    /// <summary>
    /// We must recusively get all entities in the world.
    /// starting from the world we get all root entities and 
    /// then we get all children of those entities.
    /// </summary>
    /// <returns></returns>
    private List<string> GetAllEntities()
    {
        var entities = new List<string>();
        _world.Children((Entity child) =>
        {
            entities.Add(child.Name());
            entities.AddRange(GetAllEntitiesFromEntity(child));
        });
        return entities;
    }

    private List<string> GetAllEntitiesFromEntity(Entity entity)
    {
        var entities = new List<string>();
        entity.Children((Entity child) =>
        {
            entities.Add(child.Name());
            entities.AddRange(GetAllEntitiesFromEntity(child));
        });
        return entities;
    }
}
