## Why are projects in the solution file that have nothing to do with Avalonia.Flecs?

This is because if a project is not part of the same solution it bugs out the LSP
- It cant find the source files anymore when trying to debug it correctly, it only gives you the meta data created.
- It cant show documentation proberly
- For more see: "https://github.com/dotnet/vscode-csharp/issues/6375"
    - The problem dates around one year and it hasnt change (18.12.2024) 

For this reason I include the FSRSPythonBrige Project as part of the same solution even though its only needed in the StellaLearning Project.

My personal fear is that this will grow the solution file bigger and bigger over time, but this is a mono repo anyways so... 