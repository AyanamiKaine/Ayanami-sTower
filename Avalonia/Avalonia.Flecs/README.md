# Avalonia Flecs
Avalonia Flecs introduces the ECS pattern to Avalonia as a replacement to the Model-View-ViewModel Pattern using the fantastic ECS library [Flecs](https://github.com/SanderMertens/flecs)/[DotNet Version](https://github.com/BeanCheeseBurrito/Flecs.NET?tab=readme-ov-file).

## Why?
The main reason i devloped this library is to provide a higher abstraction level with the power to freely move up and down in the abstraction. I Really wanted to just write some 100 lines of code to create a window with some buttons and fields and the ui logic all in one file. This does not mean that creating big, complex, applications is hard or not possible. See my own app for that.

## Entity Extensions
I defined various methods for entities that work with the underlying avalonia control element. For example if you attached a TextBlock control element to an entity you can write `entity.SetText("Hello")`. I did it so you dont have to write: `entity.Get<TextBlock>.Text = "Hello"`. This is also important when working with control types where you dont know the explicit type but know that it should have a Text property.

## Is AOT Supported?
No, this is because of the use of reflection for ease of use when trying to get property fields of avalonia classes where we dont know the concrete type that implements the property but know it should have a property (This relates to the various entity extensions). The same is done for method calling.