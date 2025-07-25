using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Flecs.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Flecs.NET.Core;

namespace GalaxyMapEditor
{
    // --- ECS Components ---
    // By defining our data in components, we can create systems that act on entities with specific data.

    /// <summary>
    /// A component to store the X and Y coordinates of an entity on the canvas.
    /// This is the "source of truth" for an element's position.
    /// </summary>
    public struct Position
    {
        /// <summary>
        /// X
        /// </summary>
        public double X;
        /// <summary>
        /// Y
        /// </summary>
        public double Y;
    }

    /// <summary>
    /// A tag component to identify an entity as a star system.
    /// </summary>
    public struct StarSystem { }

    /// <summary>
    /// A component that defines a connection (a line) between two entities.
    /// </summary>
    public struct Connection
    {
        /// <summary>
        /// Start entity
        /// </summary>
        public Entity Start;
        /// <summary>
        /// End point entity
        /// </summary>
        public Entity End;
    }

    /// <summary>
    /// A tag component to identify an entity as draggable.
    /// </summary>
    public struct Draggable { }

    /// <summary>
    /// A component to hold the state of the canvas's transformation (zoom and pan).
    /// </summary>
    public struct CanvasTransform
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public double Scale;
        public double TranslateX;
        public double TranslateY;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }


    /// <summary>
    /// Galaxy map editor
    /// </summary>
    public class App : Application
    {
        /// <summary>
        /// Ui Builder that represents the main window
        /// </summary>
        public UIBuilder<Window>? MainWindow;
        private static Window? _mainWindow;

        /// <summary>
        /// Returns the MainWindow
        /// </summary>
        /// <returns></returns>
        public static Window GetMainWindow() => _mainWindow!;

        private readonly World _world = World.Create();

        // --- Drag and Drop State ---
        private Entity _draggedEntity;
        private Point _dragStartPoint; // Position where the drag started relative to the control
        private Point _lastMousePosition; // Last known mouse position on the canvas for context menu

        private Entity _canvasTransformEntity; // Single entity to hold the CanvasTransform component.

        private bool _isPanning;
        private Point _panStartMousePosition;

        /// <inheritdoc/>
        public override void Initialize()
        {
            _world.Import<Avalonia.Flecs.Controls.ECS.Module>();
            _world.Import<Avalonia.Flecs.FluentUI.Controls.ECS.Module>();

            // --- Register Components ---
            // It's good practice to register components with the world.
            _world.Component<Position>();
            _world.Component<StarSystem>();
            _world.Component<Connection>();
            _world.Component<Draggable>();
            _world.Component<CanvasTransform>();

            _canvasTransformEntity = _world.Entity("CanvasTransformSingleton")
                                           .Set(new CanvasTransform { Scale = 1.0, TranslateX = 0, TranslateY = 0 });

            AvaloniaXamlLoader.Load(this);

            MainWindow = _world.UI<Window>(
                (window) =>
                {
                    window
                        .SetTitle("Galaxy Map Editor")
                        .SetHeight(600)
                        .SetWidth(800)
                        // Track mouse movement over the entire window for context menu placement
                        .OnPointerMoved((_, e) =>
                        {
                            // We get the position relative to the Canvas, which is the main grid's child
                            if (window.Get<Window>().Content is Grid g && g.Children[0] is Canvas c)
                            {
                                _lastMousePosition = e.GetPosition(c);
                            }
                        });

                    window.Child<Grid>((grid) => // Use a Grid to overlay a transparent drag layer if needed in the future
                    {
                        // The main canvas where all the action happens
                        var canvasBuilder = grid.Child<Canvas>((canvas) =>
                        {
                            canvas.SetBackground(new SolidColorBrush(Colors.Transparent)); // Ensure canvas can receive input
                                                                                           // Attach the canvas control to our transform entity so the system can find it.
                            var canvasControl = canvas.Get<Canvas>();
                            _canvasTransformEntity.Set(canvasControl);

                            // This rectangle acts as the background and the target for the "Add Star System" context menu.
                            canvas.Child<Rectangle>((rectangle) =>
                            {
                                rectangle
                                    .SetFill(new SolidColorBrush(Color.FromArgb(50, 0, 0, 20))) // A dark blue space background
                                    .SetWidth(5000)
                                    .SetHeight(5000)
                                    .SetZIndex(-10);

                                // The context menu for the background
                                var menu = _world.UI<MenuFlyout>(
                                    (menu) =>
                                    {
                                        menu.SetShowMode(FlyoutShowMode.TransientWithDismissOnPointerMoveAway);
                                        menu.Child<MenuItem>(
                                            (menuItem) =>
                                            {
                                                menuItem
                                                    .SetHeader("Add Star System")
                                                    .OnClick((_, _) =>
                                                    {
                                                        ref readonly var transform = ref _canvasTransformEntity.Get<CanvasTransform>();
                                                        // Transform mouse position from screen space to world space for correct placement.
                                                        var worldX = (_lastMousePosition.X - transform.TranslateX) / transform.Scale;
                                                        var worldY = (_lastMousePosition.Y - transform.TranslateY) / transform.Scale;
                                                        CreateStarSystem(canvas, worldX, worldY);
                                                    });
                                            }
                                        );
                                    }
                                );
                                rectangle.SetContextFlyout(menu);
                            });

                            // --- Create Initial Scene ---
                            var starSystemA = CreateStarSystem(canvas, 100, 100);
                            var starSystemB = CreateStarSystem(canvas, 300, 250);

                            // Connect the two star systems
                            ConnectTwoStarSystems(canvas, starSystemA, starSystemB);
                        });

                        grid
                                                .OnPointerWheelChanged((_, e) => HandleZoom(e))
                                                .OnPointerPressed((s, e) =>
                                                {
                                                    var pointProps = e.GetCurrentPoint(s as Visual).Properties;
                                                    if (pointProps.IsMiddleButtonPressed)
                                                    {
                                                        _isPanning = true;
                                                        _panStartMousePosition = e.GetPosition(null); // Position relative to top-level window
                                                        e.Pointer.Capture(s as InputElement);
                                                        e.Handled = true;
                                                    }
                                                })
                                                .OnPointerMoved((s, e) =>
                                                {
                                                    _lastMousePosition = e.GetPosition(s as Visual); // Track mouse for context menu
                                                    if (_isPanning)
                                                    {
                                                        HandlePan(e);
                                                    }
                                                })
                                                .OnPointerReleased((s, e) =>
                                                {
                                                    if (_isPanning)
                                                    {
                                                        _isPanning = false;
                                                        e.Pointer.Capture(null);
                                                        e.Handled = true;
                                                    }
                                                });


                    });
                });
            _mainWindow = MainWindow.Get<Window>();

            // --- Initialize ECS Systems ---
            InitializeSystems();
        }

        private void HandleZoom(PointerWheelEventArgs e)
        {
            ref var transform = ref _canvasTransformEntity.GetMut<CanvasTransform>();
            var oldScale = transform.Scale;
            var newScale = oldScale * (e.Delta.Y > 0 ? 1.1 : 1 / 1.1);
            newScale = Math.Clamp(newScale, 0.1, 5.0); // Clamp zoom level

            // Get the mouse position relative to the grid that contains the canvas
            var mousePos = e.GetPosition(e.Source as Control);

            // Calculate the new translation to keep the point under the mouse stationary
            transform.TranslateX = mousePos.X - (mousePos.X - transform.TranslateX) * (newScale / oldScale);
            transform.TranslateY = mousePos.Y - (mousePos.Y - transform.TranslateY) * (newScale / oldScale);
            transform.Scale = newScale;

            e.Handled = true;
        }

        private void HandlePan(PointerEventArgs e)
        {
            var currentMousePosition = e.GetPosition(null); // Position relative to top-level window
            var delta = currentMousePosition - _panStartMousePosition;

            ref var transform = ref _canvasTransformEntity.GetMut<CanvasTransform>();
            transform.TranslateX += delta.X;
            transform.TranslateY += delta.Y;

            _panStartMousePosition = currentMousePosition; // Update start position for next move event
        }

        /// <summary>
        /// Defines and registers the systems that will drive the editor's logic.
        /// </summary>
        private void InitializeSystems()
        {
            // This system runs for every entity that has a Position and is a Control.
            // It updates the visual position on the canvas based on the data in the Position component.
            _world.System<Position, Control>("UpdateCanvasPositions")
                .Each((Entity e, ref Position pos, ref Control ctrl) =>
                {
                    // This is the core of data-driven UI. The system reads the position data
                    // and applies it to the visual representation.
                    Canvas.SetLeft(ctrl, pos.X);
                    Canvas.SetTop(ctrl, pos.Y);
                });

            // This system updates the start and end points of any line that has a Connection component.
            _world.System<Line, Connection>("UpdateConnectionLines")
                .Each((Entity e, ref Line line, ref Connection connection) =>
                {
                    // Check if the connected entities are still alive before trying to access them.
                    if (!connection.Start.IsAlive() || !connection.End.IsAlive())
                    {
                        // If one of the connected systems is gone, destroy the line as well.
                        e.Destruct();
                        return;
                    }

                    // Get the position and control of the start and end entities.
                    ref readonly var startPos = ref connection.Start.Get<Position>();
                    var startCtrl = connection.Start.Get<Control>();

                    ref readonly var endPos = ref connection.End.Get<Position>();
                    var endCtrl = connection.End.Get<Control>();

                    // Calculate the center of each control to draw the line between centers.
                    var startPoint = new Point(startPos.X + (startCtrl.Bounds.Width / 2), startPos.Y + (startCtrl.Bounds.Height / 2));
                    var endPoint = new Point(endPos.X + (endCtrl.Bounds.Width / 2), endPos.Y + (endCtrl.Bounds.Height / 2));

                    line.StartPoint = startPoint;
                    line.EndPoint = endPoint;
                });

            _world.System<CanvasTransform, Canvas>("UpdateCanvasTransform")
            .Each((ref CanvasTransform transform, ref Canvas canvas) =>
            {
                if (canvas.RenderTransform is not TransformGroup group)
                {
                    // Initialize the transforms if they don't exist.
                    var scale = new ScaleTransform(transform.Scale, transform.Scale);
                    var translate = new TranslateTransform(transform.TranslateX, transform.TranslateY);
                    group = new TransformGroup();
                    group.Children.Add(scale);
                    group.Children.Add(translate);
                    canvas.RenderTransform = group;
                }

                var scaleTransform = group.Children[0] as ScaleTransform;
                var translateTransform = group.Children[1] as TranslateTransform;

                if (scaleTransform != null)
                {
                    scaleTransform.ScaleX = transform.Scale;
                    scaleTransform.ScaleY = transform.Scale;
                }
                if (translateTransform != null)
                {
                    translateTransform.X = transform.TranslateX;
                    translateTransform.Y = transform.TranslateY;
                }
            });

            // Set the world to run these systems automatically.
            // For a UI app, running on a timer is a good approach.
            var worldProgressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(8) // Roughly 120 FPS
            };
            worldProgressTimer.Tick += (_, _) => _world.Progress();
            worldProgressTimer.Start();
        }


        /// <summary>
        /// Creates a new star system on the canvas at the specified coordinates.
        /// </summary>
        /// <param name="parent">The canvas builder.</param>
        /// <param name="x">The initial X coordinate.</param>
        /// <param name="y">The initial Y coordinate.</param>
        /// <returns>The UIBuilder for the created star system control.</returns>
        private UIBuilder<Control> CreateStarSystem(UIBuilder<Canvas> parent, double x, double y)
        {
            // A star system is a StackPanel containing a text label and an ellipse.
            // Using a single parent control makes it easy to drag the whole group.
            return parent.Child<StackPanel>((stackPanel) =>
            {
                stackPanel.SetZIndex(1);

                // --- Add ECS Components to the Entity ---
                stackPanel.Entity
                    .Set(new Position { X = x, Y = y }) // Set initial position data.
                    .Add<StarSystem>()                  // Tag it as a StarSystem.
                    .Add<Draggable>();                  // Make it draggable.

                // Make the panel itself non-hittable so clicks go through to the children (like the ellipse)
                // but keep it enabled for layout purposes.
                stackPanel.SetIsHitTestVisible(true);

                stackPanel.Child<TextBlock>((textBlock) =>
                {
                    textBlock.SetText("Star System").SetForeground(Brushes.White).SetHorizontalAlignment(Avalonia.Layout.HorizontalAlignment.Center);
                });

                stackPanel.Child<Ellipse>((circle) =>
                {
                    circle
                        .SetFill(new SolidColorBrush(Color.FromRgb(240, 230, 140)))
                        .SetWidth(50)
                        .SetHeight(50);

                    // Context menu for deleting the star system
                    var menu = _world.UI<MenuFlyout>(
                        (menu) =>
                        {
                            menu.Child<MenuItem>(
                                (menuItem) =>
                                {
                                    menuItem
                                        .SetHeader("Remove")
                                        .OnClick((_, _) => stackPanel.Entity.Destruct()); // Destruct the whole stackpanel entity
                                });
                        }
                    );
                    circle.SetContextFlyout(menu);
                });


                // --- Drag and Drop Event Handling ---
                stackPanel
                    .OnPointerPressed((sender, e) =>
                    {
                        // Check for left-click to start drag
                        if (e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed && stackPanel.Entity.Has<Draggable>())
                        {
                            _draggedEntity = stackPanel.Entity;
                            _dragStartPoint = e.GetPosition(sender as Control); // Get position relative to the control being dragged
                            e.Pointer.Capture(sender as InputElement); // Capture the mouse to receive events even if it leaves the control
                            e.Handled = true;
                        }
                    })
                    .OnPointerMoved((sender, e) =>
                    {
                        // If we are currently dragging an entity
                        if (_draggedEntity.IsAlive() && e.Pointer.Captured != null)
                        {
                            // Get the current position of the mouse relative to the canvas
                            var currentMousePosition = e.GetPosition((sender as Visual)?.Parent as Visual);

                            // Get a mutable reference to the entity's Position component
                            ref var pos = ref _draggedEntity.GetMut<Position>();
                            pos.X = currentMousePosition.X - _dragStartPoint.X;
                            pos.Y = currentMousePosition.Y - _dragStartPoint.Y;
                        }
                    })
                    .OnPointerReleased((sender, e) =>
                    {
                        // If we were dragging an entity, release it
                        if (_draggedEntity.IsAlive() && e.Pointer.Captured != null)
                        {
                            _draggedEntity = default; // Clear the dragged entity
                            e.Pointer.Capture(null); // Release mouse capture
                            e.Handled = true;
                        }
                    });
            }).AsBaseBuilder<Control, StackPanel>();
        }

        /// <summary>
        /// Creates a line entity that dynamically connects two other entities.
        /// </summary>
        /// <param name="parent">The canvas builder.</param>
        /// <param name="starSystemA">The first star system's builder.</param>
        /// <param name="starSystemB">The second star system's builder.</param>
        private static void ConnectTwoStarSystems(UIBuilder<Canvas> parent, UIBuilder<Control> starSystemA, UIBuilder<Control> starSystemB)
        {
            parent.Child<Line>((line) =>
            {
                line
                    .SetZIndex(0)
                    .SetStroke(new SolidColorBrush(Color.FromRgb(100, 100, 200)))
                    .SetStrokeThickness(2);

                // This is the key part: we add a Connection component to the line's entity.
                // The Connection component holds references to the entities it should connect.
                // Our "UpdateConnectionLines" system will use this data to update the line's position.
                line.Entity.Set(new Connection
                {
                    Start = starSystemA.Entity,
                    End = starSystemB.Entity
                });
            });
        }

        /// <inheritdoc/>
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = MainWindow!.Get<Window>();
            }
            // this.AttachDevTools(); // Uncomment to use Avalonia DevTools
            base.OnFrameworkInitializationCompleted();
        }
    }
}
