using System.Collections.Generic;
using System.Collections.ObjectModel;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module; // Assuming this is where Page tag is defined
using Avalonia.Controls;
using Avalonia.Layout;
using NLog;
using Avalonia.Flecs.Controls; // Assuming UIBuilder and extensions are here
using Avalonia; // For Thickness

namespace StellaLearning.Pages;

/// <summary>
/// Account page, used to login, create, and manage your account.
/// Used to communicate with the backend.
/// </summary>
public class AccountPage : IUIComponent
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Create the account page UI layout.
    /// </summary>
    /// <param name="world">The Flecs world.</param>
    public AccountPage(World world)
    {
        // Create the root Grid for the page layout
        _root = world.UI<Grid>((grid) =>
        {
            grid
                // Set padding around the entire grid content
                //.SetPadding(20)
                // Center the content vertically and horizontally within the available space
                .SetVerticalAlignment(VerticalAlignment.Center)
                .SetHorizontalAlignment(HorizontalAlignment.Center)
                // Define rows: Auto height for title, fields, buttons, and spacing
                .SetRowDefinitions("Auto, Auto, Auto, 20, Auto, Auto, 20, Auto")
                // Define one column that stretches to fill width
                .SetColumnDefinitions("*");

            // --- Title ---
            grid.Child<TextBlock>(title =>
            {
                title
                    .SetRow(0) // Place in the first row
                    .SetText("Account") // Set the title text
                    .SetFontSize(24) // Set a larger font size for the title
                    .SetFontWeight(Avalonia.Media.FontWeight.Bold) // Make the title bold
                    .SetHorizontalAlignment(HorizontalAlignment.Center); // Center the title horizontally
            });

            // --- Username/Email Field ---
            grid.Child<TextBlock>(usernameLabel =>
            {
                usernameLabel
                    .SetRow(1) // Place in the second row
                    .SetText("Username or Email:") // Label text
                    .SetMargin(0, 10, 0, 5); // Add some margin top and bottom
            });
            grid.Child<TextBox>(usernameInput =>
            {
                usernameInput
                    .SetRow(2) // Place below the label
                    .SetWatermark("Enter your username or email"); // Placeholder text
                                                                   // No OnTextChanged or binding - layout only
            });

            // --- Password Field ---
            // Row 3 is skipped for spacing (defined in RowDefinitions as "20")
            grid.Child<TextBlock>(passwordLabel =>
            {
                passwordLabel
                    .SetRow(4) // Place in the fifth row
                    .SetText("Password:") // Label text
                    .SetMargin(0, 0, 0, 5); // Add some margin bottom
            });
            grid.Child<TextBox>(passwordInput =>
            {
                passwordInput
                    .SetRow(5) // Place below the label
                    .SetWatermark("Enter your password") // Placeholder text
                    .With(tb => tb.PasswordChar = '*') // Use With() for direct property access
                                                       // No OnTextChanged or binding - layout only
                    ;
            });

            // --- Action Buttons ---
            // Row 6 is skipped for spacing (defined in RowDefinitions as "20")
            grid.Child<StackPanel>(buttonPanel =>
            {
                buttonPanel
                    .SetRow(7) // Place in the last row
                    .SetOrientation(Orientation.Horizontal) // Arrange buttons horizontally
                    .SetHorizontalAlignment(HorizontalAlignment.Center) // Center the panel
                    .SetSpacing(10); // Add space between buttons

                // Login Button
                buttonPanel.Child<Button>(loginButton =>
                {
                    loginButton
                        .SetMinWidth(100) // Set a minimum width
                        .SetText("Login");
                    // No OnClick handler - layout only
                });

                // Register Button
                buttonPanel.Child<Button>(registerButton =>
                {
                    registerButton
                        .SetMinWidth(100) // Set a minimum width
                        .SetText("Register");
                    // No OnClick handler - layout only
                });

                // Optionally, add a "Forgot Password" link/button if needed
                // buttonPanel.Child<Button>(forgotPasswordButton => { ... });
            });

        })
        .Add<Page>() // Add the Page tag to identify this as a page component
        .Entity; // Get the final entity from the builder
    }

    /// <inheritdoc/>
    public void Attach(Entity entity)
    {
        // Check if the root entity is valid and alive before attaching
        if (_root.IsValid() && _root.IsAlive())
        {
            _root.ChildOf(entity);
            Logger.Trace($"AccountPage (Root: {_root.Id}) attached to Parent: {entity.Id}");
        }
        else
        {
            Logger.Warn($"Attempted to attach invalid AccountPage (Root: {_root.Id}) to Parent: {entity.Id}");
        }
    }

    /// <inheritdoc/>
    public void Detach()
    {
        // Check if the root entity is valid and alive before detaching
        if (_root.IsValid() && _root.IsAlive())
        {
            // Check if it actually has a parent before trying to remove the relationship
            if (_root.Has(Ecs.ChildOf, Ecs.Wildcard))
            {
                _root.Remove(Ecs.ChildOf, Ecs.Wildcard); // Use Wildcard to remove relationship regardless of parent ID
                Logger.Trace($"AccountPage (Root: {_root.Id}) detached.");
            }
            else
            {
                Logger.Trace($"AccountPage (Root: {_root.Id}) already detached or had no parent.");
            }
        }
        else
        {
            Logger.Warn($"Attempted to detach invalid AccountPage (Root: {_root.Id})");
        }
    }
}
