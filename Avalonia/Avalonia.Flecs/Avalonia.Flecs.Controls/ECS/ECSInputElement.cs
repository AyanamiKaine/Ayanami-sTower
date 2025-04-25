using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the InputElement component
    /// </summary>
    public class ECSInputElement : IFlecsModule
    {
        /// <summary>
        /// Initializes the InputElement component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {
            world.Module<ECSInputElement>();
            world
                .Component<InputElement>("InputElement")
                .OnSet(
                    (Entity e, ref InputElement inputElement) =>
                    {
                        if (!e.Has<object>())
                        {
                            e.Set<object>(inputElement);
                        }
                        else if (e.Get<object>().GetType() == typeof(InputElement))
                        {
                            e.Set<object>(inputElement);
                        }
                        e.Set<Interactive>(inputElement);
                        //We set the Layoutable and Visual components to the same instance of the InputElement instead of seperate ECS modules because
                        //otherwise it would result into an stack overflow!
                        e.Set<Layoutable>(inputElement);
                        e.Set<Visual>(inputElement);

                        // IMPORTANT
                        // ALL OBERSERVES RUN IN A NON-UI THREAD THIS IS THE DEFAULT BEHAVIOR IN AVALONIA
                        // ANY CODE EXECUTED IN AN OBSERVE THAT MODIFIES THE UI MUST BE DISPATCHED TO THE UI THREAD
                        // THIS CAN BE DONE BY USING THE
                        // Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { /* UI CODE HERE */ });
                        // THIS ALSO MATTER FOR ALL FUNCTIONS
                        // THAT WANT TO USE THE ECS WORLD FOUND IN MAIN THE APPLICATION
                    }
                )
                .OnRemove(
                    (Entity e, ref InputElement inputElement) =>
                    {
                        e.Remove<Interactive>();
                        e.Remove<Layoutable>();
                        e.Remove<Visual>();
                    }
                );
        }
    }
}
