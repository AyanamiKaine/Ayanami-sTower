using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Animation;
using Flecs.NET.Core;

namespace Avalonia.Flecs.Controls.ECS
{
    public static class ECSAnimationExtensions
    {
        public static Entity AddAnimation(this Entity entity, string animationName, Animation.Animation animation)
        {
            if (entity.Has<Control>())
            {
                entity.Get<Control>().Resources.Add(animationName, animation);
                return entity;
            }
            else if (entity.Has<object>())
            {
                var method = entity.GetProperty<object>("Resources").GetType().GetMethod("Add") ?? throw new MissingMethodException($"Method Add not found in Resources object");
                method.Invoke(entity.GetProperty<object>("Resources"), [animationName, animation]);

                return entity;
            }
            throw new ComponentNotFoundException(entity, typeof(Control), nameof(AddAnimation));
        }

        public static Animation.Animation? GetAnimation(this Entity entity, string animationName)
        {
            if (entity.Has<Control>())
            {
                return entity.Get<Control>().Resources[animationName] as Animation.Animation;
            }
            else if (entity.Has<object>())
            {
                //This does not seem correct.
                //return entity.GetProperty<object>("Resources").GetType().GetMethod("get_Item").Invoke(entity.GetProperty<object>("Resources"), [animationName]) as Animation.Animation;
            }
            throw new ComponentNotFoundException(entity, typeof(Control), nameof(GetAnimation));
        }
    }
}