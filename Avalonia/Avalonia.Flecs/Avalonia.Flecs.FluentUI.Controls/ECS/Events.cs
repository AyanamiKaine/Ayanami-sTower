using Avalonia.Controls;
using Flecs.NET.Core;
using FluentAvalonia.UI.Controls;

namespace Avalonia.Flecs.FluentUI.Controls.ECS.Events
{
    public record struct OnSelectionChanged(Object Sender, NavigationViewSelectionChangedEventArgs Args);
}