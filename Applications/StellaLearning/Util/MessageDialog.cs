/*
<one line to give the program's name and a brief idea of what it does.>
Copyright (C) <2025>  <Patrick, Grohs>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using Avalonia.Controls;
using Avalonia.Flecs.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;

namespace AyanamisTower.StellaLearning.Util;

/// <summary>
/// Utility class for displaying message dialogs to the user.
/// </summary>
public static class MessageDialog
{
    /// <summary>
    /// Shows a dialog with the specified title and message.
    /// This method runs on the UI thread with background priority.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to display in the dialog.</param>
    public static void ShowDialog(string title, string message)
    {
        Dispatcher.UIThread.InvokeAsync(
            () =>
            {
                var cd = new ContentDialog()
                {
                    Title = title,
                    Content = message,
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                    IsSecondaryButtonEnabled = true,
                };
                cd.ShowAsync();
            },
            DispatcherPriority.Background
        );
    }

    /// <summary>
    /// Shows an error dialog with the specified message.
    /// This method runs on the UI thread with background priority.
    /// </summary>
    /// <param name="message">The error message to display in the dialog.</param>
    public static void ShowErrorDialog(string message)
    {
        Dispatcher.UIThread.InvokeAsync(
            () =>
            {
                var cd = new ContentDialog()
                {
                    Title = new TextBlock() { Text = "Error!", Foreground = Brushes.Red },
                    Content = message,
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                    IsSecondaryButtonEnabled = true,
                };
                cd.ShowAsync();
            },
            DispatcherPriority.Background
        );
    }

    /// <summary>
    /// Shows a warning dialog with the specified message.
    /// This method runs on the UI thread with background priority.
    /// </summary>
    /// <param name="message">The warning message to display in the dialog.</param>
    public static void ShowWarningDialog(string message)
    {
        Dispatcher.UIThread.InvokeAsync(
            () =>
            {
                var cd = new ContentDialog()
                {
                    Title = new TextBlock() { Text = "Warning", Foreground = Brushes.Goldenrod },
                    Content = message,
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                    IsSecondaryButtonEnabled = true,
                };
                cd.ShowAsync();
            },
            DispatcherPriority.Background
        );
    }

    /// <summary>
    /// Shows a dialog with the specified title and custom content created by the UI builder.
    /// This method runs on the UI thread with background priority.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="uiBuilder">The UI builder that creates the content for the dialog.</param>
    /// <typeparam name="T">The type of control created by the UI builder.</typeparam>
    public static void ShowDialog<T>(string title, UIBuilder<T> uiBuilder)
        where T : Control
    {
        Dispatcher.UIThread.InvokeAsync(
            () =>
            {
                var cd = new ContentDialog()
                {
                    Title = title,
                    Content = uiBuilder.Get<T>(),
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                    IsSecondaryButtonEnabled = true,
                };
                cd.ShowAsync();
            },
            DispatcherPriority.Background
        );
    }

    /// <summary>
    /// Shows an error dialog with custom content created by the UI builder.
    /// This method runs on the UI thread with background priority.
    /// </summary>
    /// <param name="uiBuilder">The UI builder that creates the content for the dialog.</param>
    /// <typeparam name="T">The type of control created by the UI builder.</typeparam>
    public static void ShowErrorDialog<T>(UIBuilder<T> uiBuilder)
        where T : Control
    {
        Dispatcher.UIThread.InvokeAsync(
            () =>
            {
                var cd = new ContentDialog()
                {
                    Title = new TextBlock() { Text = "Error!", Foreground = Brushes.Red },
                    Content = uiBuilder.Get<T>(),
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                    IsSecondaryButtonEnabled = true,
                };
                cd.ShowAsync();
            },
            DispatcherPriority.Background
        );
    }

    /// <summary>
    /// Shows a warning dialog with custom content created by the UI builder.
    /// This method runs on the UI thread with background priority.
    /// </summary>
    /// <param name="uiBuilder">The UI builder that creates the content for the dialog.</param>
    /// <typeparam name="T">The type of control created by the UI builder.</typeparam>
    public static void ShowWarningDialog<T>(UIBuilder<T> uiBuilder)
        where T : Control
    {
        Dispatcher.UIThread.InvokeAsync(
            () =>
            {
                var cd = new ContentDialog()
                {
                    Title = new TextBlock() { Text = "Warning", Foreground = Brushes.Goldenrod },
                    Content = uiBuilder.Get<T>(),
                    PrimaryButtonText = "Ok",
                    DefaultButton = ContentDialogButton.Primary,
                    IsSecondaryButtonEnabled = true,
                };
                cd.ShowAsync();
            },
            DispatcherPriority.Background
        );
    }
}
