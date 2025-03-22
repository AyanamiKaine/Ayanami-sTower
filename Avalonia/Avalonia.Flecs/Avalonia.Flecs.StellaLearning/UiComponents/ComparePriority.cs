using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Flecs.Controls.ECS;
using Avalonia.Flecs.StellaLearning.Data;
using Flecs.NET.Core;
using System.Reactive.Disposables;
using NLog;

namespace Avalonia.Flecs.StellaLearning.UiComponents;

/// <summary>
/// Ui component
/// </summary>
public class ComparePriority : IUIComponent, IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private ObservableCollection<SpacedRepetitionItem> _spacedRepetitionItems = [];
    private Entity _root;
    private Entity _calculatedPriorityEntity;
    /// <summary>
    /// Returns the entity that holds the calculatedPriority
    /// </summary>
    public Entity CalculatedPriorityEntity => _calculatedPriorityEntity;
    /// <inheritdoc/>
    public Entity Root => _root;
    private SpacedRepetitionItem? _currentItemToCompare = null;
    private string? _currentItemName = "";
    private Random _rng = new();
    private bool isDisposed = false;

    /// <summary>
    /// We want that the user can only compare two times, no need to compare to all items
    /// </summary>
    private int _timesCompared = 0;

    private int _heighestPossiblePriority = 999999999;
    private int _smallestPossiblePriority = 0;

    private UIBuilder<Button>? morePriorityButton;
    private UIBuilder<Button>? lessPriorityButton;
    private UIBuilder<TextBlock>? itemToCompareToTextBlock;
    // Collection to track all disposables
    private readonly CompositeDisposable _disposables = [];

    /// <summary>
    /// Creates A ComparePriority component
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public ComparePriority(World world)
    {
        // We want that the user can only compare two times, no need to compare to all items

        _spacedRepetitionItems = world.Get<ObservableCollection<SpacedRepetitionItem>>();

        //We are doing so the calucalted priority value is always reflected as the newest one.
        _calculatedPriorityEntity = world.Entity()
            .Set(500000000);

        _root = world.UI<Grid>((grid) =>
        {
            grid
                .SetColumnDefinitions("*,*")
                .SetRowDefinitions("*,*,*");

            grid.Child<TextBlock>((textBlock) =>
            {
                textBlock
                .SetTextWrapping(Media.TextWrapping.Wrap)
                .SetVerticalAlignment(Layout.VerticalAlignment.Center)
                .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                .SetRow(0)
                .SetColumnSpan(2)
                .SetText("Is the new item more or less important than this one?");
            });
            grid.Child<Button>((button) =>
            {
                lessPriorityButton = button;
                button.Child<TextBlock>((t) => t.SetText("Less"));
                button.SetHorizontalAlignment(Layout.HorizontalAlignment.Left)
                .SetMargin(20)
                .SetColumn(0)
                .SetRow(2);

                button.OnClick((_, _) =>
                {
                    _timesCompared++;

                    CalculatedPriorityEntity.Set<int>(_currentItemToCompare!.Priority - _rng.Next(50));

                    _heighestPossiblePriority = CalculatedPriorityEntity.Get<int>();

                    var itemsBetweenLowAndHighPriority = _spacedRepetitionItems
                        .Where(x => x.Priority >= _smallestPossiblePriority && x.Priority <= _heighestPossiblePriority);

                    _currentItemToCompare = itemsBetweenLowAndHighPriority
                        .OrderBy(x => x.Priority > _heighestPossiblePriority)
                        .Reverse()
                        .FirstOrDefault();

                    if (_currentItemToCompare is null || _currentItemToCompare.Priority > CalculatedPriorityEntity.Get<int>())
                    {
                        if (_timesCompared >= 3)
                        {
                            _currentItemName = "Compared Enough Items";
                        }
                        else
                        {
                            _currentItemName = "No more items to compare to";
                        }

                        lessPriorityButton!.Disable();
                        morePriorityButton!.Disable();
                    }
                    else
                    {
                        _currentItemName = _currentItemToCompare.Name;
                    }
                    itemToCompareToTextBlock!.SetText(_currentItemName);
                });
            });

            grid.Child<Button>((button) =>
            {
                morePriorityButton = button;
                button.Child<TextBlock>(t => t.SetText("More"));
                button.SetHorizontalAlignment(Layout.HorizontalAlignment.Right)
                .SetMargin(20)
                .SetColumn(1)
                .SetRow(2);
                button.OnClick((_, _) =>
                {
                    _timesCompared++;

                    if (_currentItemToCompare is not null)
                        CalculatedPriorityEntity.Set<int>(_currentItemToCompare!.Priority + _rng.Next(50));
                    else
                        CalculatedPriorityEntity.Set<int>(_rng.Next(1000));

                    _smallestPossiblePriority = CalculatedPriorityEntity.Get<int>();

                    _currentItemToCompare = _spacedRepetitionItems
                        .Where(x => x.Priority >= _smallestPossiblePriority && x.Priority <= _heighestPossiblePriority)
                        .OrderBy(x => x.Priority)
                        .FirstOrDefault();

                    if (_currentItemToCompare is null || _currentItemToCompare.Priority < CalculatedPriorityEntity.Get<int>())
                    {
                        if (_timesCompared >= 3)
                        {
                            _currentItemName = "Compared Enough Items";
                        }
                        else
                        {
                            _currentItemName = "No more items to compare to";
                        }

                        lessPriorityButton!.Disable();
                        morePriorityButton!.Disable();
                    }
                    else
                    {
                        _currentItemName = _currentItemToCompare.Name;
                    }
                    itemToCompareToTextBlock!.SetText(_currentItemName);
                });
            });

            grid.Child<TextBlock>((text) =>
            {
                itemToCompareToTextBlock = text;

                if (_spacedRepetitionItems?.Count != 0 && _spacedRepetitionItems is not null)
                {
                    _currentItemToCompare = _spacedRepetitionItems.OrderBy(x => _rng.Next()).First();
                    _currentItemName = _currentItemToCompare.Name;
                }
                else
                {
                    morePriorityButton!.Disable();
                    lessPriorityButton!.Disable();
                    _currentItemToCompare = null;
                    _currentItemName = "No Items to compare to";
                }

                text.SetVerticalAlignment(Layout.VerticalAlignment.Center)
                .SetHorizontalAlignment(Layout.HorizontalAlignment.Center)
                .SetMargin(20)
                .SetRow(1)
                .SetColumnSpan(2)
                .SetText(_currentItemName!)
                .SetTextWrapping(Media.TextWrapping.Wrap)
                .SetFontWeight(Media.FontWeight.Bold);
            });
        });

        /*
          Here we define the things we want to dispose when the window closes,
          we mostly want to removed event handlers and destroy the entity
          */

        _spacedRepetitionItems!.CollectionChanged += OnCollectionChanged;
        _disposables.Add(Disposable.Create(() =>
            _spacedRepetitionItems!.CollectionChanged -= OnCollectionChanged));

        _disposables.Add(Disposable.Create(() =>
        {
            if (_root.IsValid()) _root.Destruct();
        }));

        _calculatedPriorityEntity = CalculatedPriorityEntity;
        _root.SetName($"COMPAREPRIORITY-{_rng.Next()}");
    }

    /// <summary>
    /// Resets the priority compare component;
    /// </summary>
    public void Reset()
    {
        _heighestPossiblePriority = 999999999;
        _smallestPossiblePriority = 0;
        morePriorityButton!.Enable();
        lessPriorityButton!.Enable();
        _timesCompared = 0;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources; 
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                // Dispose all tracked disposables
                _disposables.Dispose();
            }

            isDisposed = true;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        //Defensive programming can help medicating the problem.
        if (!_root.IsValid() || !_root.IsAlive() || _root == 0)
            return;

        if (_spacedRepetitionItems?.Count != 0 && _spacedRepetitionItems is not null)
        {
            var randomIndex = _rng.Next(_spacedRepetitionItems.Count);
            _currentItemToCompare = _spacedRepetitionItems[randomIndex];

            if (_currentItemToCompare is not null)
                _currentItemName = _currentItemToCompare.Name;
            else
                _currentItemName = "No Items to compare to";

            itemToCompareToTextBlock!.SetText(_currentItemName);
        }
        else
        {
            _currentItemToCompare = null;
            _currentItemName = "No Items to compare to";
            itemToCompareToTextBlock!.SetText(_currentItemName);
        }
    }
}