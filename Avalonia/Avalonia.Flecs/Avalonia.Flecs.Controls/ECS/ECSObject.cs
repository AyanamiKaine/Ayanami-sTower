using Flecs.NET.Core;
namespace Avalonia.Flecs.Controls.ECS
{
    /// <summary>
    /// This ECS Module is used to register the Object component
    /// </summary>
    public class ECSObject : IFlecsModule
    {
        /// <summary>
        /// Initializes the Object component
        /// </summary>
        /// <param name="world"></param>
        public void InitModule(World world)
        {

            /*
            Why do we create a object component?
            We are doing this to better model existing 
            Object Hierarchies. One problem i encountered
            was having entity extension functions that need to 
            work with many different components. But each component
            is expected to be the only one. So no entity has a two control
            component attached. This is because avalonia expects 
            controls to be more modeled via OOP inheritance.

            We always set a control to also be an object, if an 
            entity function like SetText tries to 
            it will first check for component types with a text field.
            If it finds one it will set the text field. If it doesn't
            it will check for a object component and use reflection to
            find out if it has the expected property. If it doesnt it will 
            probably throw an exception if the property is not found.
            */

            world.Module<ECSObject>();
            world.Component<object>("Object");
        }
    }
}