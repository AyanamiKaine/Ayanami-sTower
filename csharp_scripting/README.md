## Using C# as a Scripting Language

Many applications allow using a higher level scripting language to interact with the application code at runtime. Many example include ELisp in Emacs, Lua in Games, Neovim, Server, and many more languages and use cases.

But C# is also totally valid as a scripting language not only for C++/C project but also for C# projects itself. It enables sandboxed c# execution at runtime. So you can write the application in C# and expose an API over to a C# Scripting Environment that can use those defined APIs at runtime.

Here we go over some experimentation and use cases what we can do and how we can do it.