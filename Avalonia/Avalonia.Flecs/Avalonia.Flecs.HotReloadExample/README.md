# Hot Reload Setup for Avalonia.Flecs UIBuilder

This document explains the hot reload mechanism used in this project to enable live UI updates during development when modifying C# code that programmatically defines the user interface using Avalonia and Flecs.NET.

## Goal

The primary goal is to modify the C# code that defines UI structure (e.g., within a method like `MainContent` in `App.cs`) and see the changes reflected in the running application _without requiring a full application restart_. This significantly speeds up the UI development workflow.

A specific challenge with UI built programmatically via an Entity Component System (ECS) like Flecs.NET is that simple code patching might not be sufficient. Structural changes often require specific cleanup of old UI entities and setup of new ones, which standard hot reload might not handle automatically for custom abstractions like our `UIBuilder`.

## The Combined Approach

This project achieves robust hot reloading by combining two key mechanisms:

1.  **Built-in .NET Hot Reload:** Leveraged via the `dotnet watch run` command.
2.  **Manual File System Watcher:** Implemented within `App.cs` (guarded by `#if DEBUG`) to trigger custom rebuild logic.

## How it Works

Here's the sequence of events when a relevant C# source file (e.g., `App.cs` containing the `MainContent` method) is modified and saved while the application is running via `dotnet watch run`:

1.  **Code Update (`dotnet watch`):**

    - `dotnet watch` detects the file change.
    - It recompiles the necessary code changes.
    - Crucially, it **patches the running application's memory**, updating the loaded implementation of methods like `MainContent` to the new version.

2.  **Rebuild Trigger (`FileSystemWatcher`):**

    - The `FileSystemWatcher` instance within the running `App.axaml.cs` _also_ detects the file save operation on disk.
    - It triggers an event, which (after a short debounce period to prevent rapid firing) calls the custom `RebuildMainWindowContent` method.

3.  **Custom Rebuild Execution (`RebuildMainWindowContent`):**
    - This method runs on the Avalonia UI thread.
    - **Cleanup:** It first finds the Flecs `Entity` representing the _old_ UI content (tracked via the `_currentContentEntity` field). It then calls `.Destruct()` on this old entity. This leverages Flecs observers (`OnRemove`) configured in the `Avalonia.Flecs.Controls.ECS.Module` to automatically clean up associated Avalonia controls and resources (like event subscriptions).
    - **Execute New Code:** It then calls the `MainContent()` method. Because `.NET Hot Reload` (Step 1) already updated the application's code in memory, this call now executes the **newly saved version** of `MainContent`.
    - **Create New Structure:** The updated `MainContent` returns a `UIBuilder` representing the _new_ desired UI structure, backed by new Flecs entities. The entity ID of this new content root is stored in `_currentContentEntity`.
    - **Attach & Update UI:** The new content entity is attached as a child to the main window's entity in the Flecs hierarchy. Flecs observers (like `ControlToParentAdder`) detect this relationship change and automatically add the corresponding new Avalonia control(s) to the main window, making the updated UI visible.

## Why This Combination?

- **.NET Hot Reload is Essential:** It provides the indispensable mechanism for updating the C# code _within the running process_. Without it, the `MainContent` method would always execute its original, compiled version.
- **Manual Rebuild Logic is Necessary:** For this specific `Avalonia.Flecs` pattern, simply patching the code might not be enough for _structural_ UI changes. The manual `RebuildMainWindowContent` method provides the explicit steps needed to:
  - Gracefully **destroy the old Flecs entity hierarchy**, ensuring proper cleanup via Flecs observers.
  - **Instantiate the new Flecs entity hierarchy** based on the updated code.
  - **Correctly attach** the new hierarchy within Flecs, allowing observers to update the Avalonia UI tree.

This synergy provides a robust hot reload experience tailored to the needs of this programmatic, ECS-driven UI approach.

## Implementation Details

- The `FileSystemWatcher` and associated rebuild logic reside in `App.axaml.cs` and are wrapped in `#if DEBUG` directives to exclude them from release builds.
- A debounce mechanism (`System.Threading.Timer`) is used to prevent excessive rebuild attempts during rapid file saves.
- UI updates are dispatched to the Avalonia UI thread using `Dispatcher.UIThread.Post`.
- The `_currentContentEntity` field in `App.axaml.cs` is crucial for tracking the root entity of the UI section being reloaded.
- Ensure the `FileSystemWatcher` is configured to watch the correct source file containing the UI definition method (`MainContent`).

## How to Use

1.  Navigate to the project directory in your terminal.
2.  Run the application using the command:
    ```bash
    dotnet watch run
    ```
3.  Wait for the application to start.
4.  Open the relevant C# source file (e.g., `App.axaml.cs`) in your editor.
5.  Modify the code within the `MainContent` method (or your designated UI definition method). You can change control properties, add new controls, remove existing ones, or modify logic.
6.  Save the file.
7.  Observe the console output from `dotnet watch` (indicating updates) and the application window – the UI should update to reflect your code changes after a brief delay.

Enjoy the faster development cycle!
