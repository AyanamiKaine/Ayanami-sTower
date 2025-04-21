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
using System;
using Flecs.NET.Core;
using static Avalonia.Flecs.Controls.ECS.Module; // Assuming this is where Page tag is defined
using Avalonia.Controls;
using Avalonia.Layout;
using NLog;
using Avalonia.Flecs.Controls; // Assuming UIBuilder and extensions are here
using Avalonia.Threading; // For Dispatcher
using System.Net.Http; // For HttpClient
using System.Net.Http.Json; // For PostAsJsonAsync, ReadFromJsonAsync
using System.Text.Json; // For JsonSerializerOptions
using AyanamisTower.WebAPI.Dtos; // Include the DTOs namespace
using Avalonia.Media; // For Brushes

namespace AyanamisTower.StellaLearning.Pages;

/// <summary>
/// Account page, used to login, create, and manage your account.
/// Handles communication with the backend API.
/// </summary>
public class AccountPage : IUIComponent
{
    private Entity _root;
    /// <inheritdoc/>
    public Entity Root => _root;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // --- Backend ---
    // Use a static HttpClient for performance and connection reuse.
    // Consider configuring BaseAddress, DefaultRequestHeaders (e.g., User-Agent) in a central place.
    private static readonly HttpClient HttpClient = new();
    private const string BackendBaseUrl = "http://localhost:5070"; // From launchSettings.json (use HTTPS if available/configured)

    // --- UI Element Builders (Store references to access input/output) ---
    //private bool _loggedIn = false;
    private UIBuilder<TextBox>? _usernameInputBuilder;
    private UIBuilder<TextBox>? _passwordInputBuilder;
    private UIBuilder<TextBox>? _confirmPasswordInputBuilder; // Added for registration
    private UIBuilder<TextBlock>? _statusMessageBuilder; // To display feedback

    // --- State (Example - In a real app, manage this better) ---
    private string? _authToken;
    private AuthResponseDto? _currentUser;

    /// <summary>
    /// Create the account page UI layout and add interaction logic.
    /// </summary>
    /// <param name="world">The Flecs world.</param>
    public AccountPage(World world)
    {
        _root = world.UI<StackPanel>((stack) =>
        {
            stack
                .SetSpacing(15)
                .SetVerticalAlignment(VerticalAlignment.Center)
                .SetHorizontalAlignment(HorizontalAlignment.Center);

            // --- Title ---
            stack.Child<TextBlock>(title =>
            {
                title
                    .SetRow(0)
                    .SetText("Account")
                    .SetFontSize(24)
                    .SetFontWeight(FontWeight.Bold)
                    .SetHorizontalAlignment(HorizontalAlignment.Center);
            });

            // --- Username/Email Field ---

            var usernameLabel = stack.Child<TextBlock>(usernameLabel =>
            {
                usernameLabel
                    .Visible(false)
                    .SetText("Email:")
                    .SetMargin(0, 15, 0, 5); // Increased top margin
            });
            var usernameInput = stack.Child<TextBox>(usernameInput =>
            {
                _usernameInputBuilder = usernameInput; // Store reference
                usernameInput
                    .Visible(false)
                    .SetWatermark("Enter your email");
            });

            // --- Password Field ---
            // Row 3 is spacing
            var passwordLabel = stack.Child<TextBlock>(passwordLabel =>
            {
                passwordLabel
                    .Visible(false)
                    .SetText("Password:")
                    .SetMargin(0, 0, 0, 5);
            });
            var passwordInput = stack.Child<TextBox>(passwordInput =>
            {
                _passwordInputBuilder = passwordInput; // Store reference
                passwordInput
                    .Visible(false)
                    .SetRow(5)
                    .SetWatermark("Enter your password")
                    .With(tb => tb.PasswordChar = '*');
            });

            // --- Confirm Password Field (for Registration) ---
            // Row 6 is spacing
            var confirmPasswordLabel = stack.Child<TextBlock>(confirmPasswordLabel =>
            {
                confirmPasswordLabel
                    .Visible(false)
                    .SetRow(7)
                    .SetText("Confirm Password:")
                    .SetMargin(0, 0, 0, 5);
            });
            var confirmPasswordInput = stack.Child<TextBox>(confirmPasswordInput =>
            {
                _confirmPasswordInputBuilder = confirmPasswordInput; // Store reference
                confirmPasswordInput
                    .SetRow(8)
                    .Visible(false)
                    .SetWatermark("Confirm your password")
                    .With(tb => tb.PasswordChar = '*');
            });

            // --- Action Buttons ---
            // Row 9 is spacing
            stack.Child<StackPanel>(buttonPanel =>
            {
                buttonPanel
                    .SetRow(10) // Adjusted row index
                    .SetOrientation(Orientation.Horizontal)
                    .SetHorizontalAlignment(HorizontalAlignment.Center)
                    .SetSpacing(10);

                buttonPanel.Child<Button>(loginButton =>
                {
                    loginButton
                        .SetMinWidth(100)
                        .SetText("Sign in")
                        .OnClick((sender, e) =>
                        {
                            // We can only login when the the confirm password input
                            // is not visible, this happens when we click the
                            // button again. 

                            if (!confirmPasswordInput.IsVisible() && usernameLabel.IsVisible())
                            {
                                HandleLoginClick(sender, e);
                            }

                            usernameLabel.Visible();
                            usernameInput.Visible();
                            passwordInput.Visible();
                            passwordLabel.Visible();
                            confirmPasswordLabel.Visible(false);
                            confirmPasswordInput.Visible(false);
                        });
                });

                // Register Button
                buttonPanel.Child<Button>(registerButton =>
                {
                    registerButton
                        .SetMinWidth(100)
                        .SetText("Sign up")
                        .OnClick((sender, e) =>
                        {

                            // We can only register when the the confirm password input
                            // is visible

                            if (confirmPasswordInput.IsVisible() && usernameLabel.IsVisible())
                            {
                                HandleRegisterClick(sender, e);
                            }

                            usernameLabel.Visible();
                            usernameInput.Visible();
                            passwordInput.Visible();
                            passwordLabel.Visible();
                            confirmPasswordLabel.Visible();
                            confirmPasswordInput.Visible();
                        });
                });
            });

            // --- Status Message Area ---
            stack.Child<TextBlock>(status =>
            {
                _statusMessageBuilder = status; // Store reference
                status
                   .SetRow(11) // Place below buttons
                   .SetMargin(0, 15, 0, 0) // Add margin above
                   .SetHorizontalAlignment(HorizontalAlignment.Center)
                   .SetTextWrapping(TextWrapping.Wrap); // Wrap long messages
                                                        // Initial text is empty
            });

        })
        .Add<Page>()
        .Entity;
    }

    /// <summary>
    /// Handles the click event for the Login button.
    /// Refactored for clearer error handling based on API response.
    /// </summary>
    private async void HandleLoginClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Defensive checks for UI elements
        if (_usernameInputBuilder == null || !_usernameInputBuilder.Entity.IsAlive() ||
            _passwordInputBuilder == null || !_passwordInputBuilder.Entity.IsAlive())
        {
            SetStatusMessage("Internal error: Input fields not ready.", true);
            Logger.Error("Login attempt failed: UI input builders are null or their entities are dead.");
            return;
        }

        string email = _usernameInputBuilder.GetText()?.Trim() ?? string.Empty;
        string password = _passwordInputBuilder.GetText() ?? string.Empty; // Don't trim password

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            SetStatusMessage("Please enter both email and password.", true);
            return;
        }

        var loginDto = new LoginDto { Email = email, Password = password };
        SetStatusMessage("Logging in...", false); // Indicate progress

        HttpResponseMessage? response = null;
        try
        {
            string loginUrl = $"{BackendBaseUrl}/api/auth/login";
            Logger.Info($"Attempting login to: {loginUrl} for user {email}");

            // Use PostAsJsonAsync for simplicity (handles serialization and Content-Type)
            response = await HttpClient.PostAsJsonAsync(loginUrl, loginDto);

            Logger.Debug($"Login response status code: {response.StatusCode}");

            if (response.IsSuccessStatusCode) // Status 200 OK
            {
                // Attempt to deserialize the successful response
                AuthResponseDto? authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (authResponse != null && !string.IsNullOrEmpty(authResponse.Token))
                {
                    _authToken = authResponse.Token;
                    _currentUser = authResponse;
                    Logger.Info($"Login successful for {authResponse.Email}. Token expires at {authResponse.TokenExpiration}.");
                    SetStatusMessage($"Welcome {authResponse.Email}!", false);

                    // --- IMPORTANT: Securely store the token ---
                    // In a real application, use platform-specific secure storage
                    // (e.g., Windows Credential Manager, macOS Keychain, SecureStorage in MAUI/Xamarin)
                    // DO NOT store tokens in plain text files or user settings directly.
                    // Example placeholder: await SecureStorage.SetAsync("auth_token", _authToken);
                    Logger.Info("TODO: Implement secure token storage.");


                    // TODO: Update application state (e.g., navigate to main page, update UI)
                    // Example: _world.Emit<UserLoggedInEvent>(new UserLoggedInEvent(_currentUser));
                    // Example: NavigateToMainPage();
                }
                else
                {
                    // This case should ideally not happen if the API returns 200 OK with a valid body
                    Logger.Error("Login successful (200 OK) but failed to deserialize response body or token is missing.");
                    SetStatusMessage("Login failed: Could not process server response.", true);
                }
            }
            else // Handle non-success status codes (400, 401, 500, etc.)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                Logger.Warn($"Login failed. Status: {response.StatusCode}, Response Body: {responseBody}");

                // Default error message
                string errorMessage = "Login failed. Please check credentials or server status.";

                // Try to parse specific error messages from the backend
                // This relies on the backend consistently returning the ErrorDto or MessageDto structure on failure
                try
                {
                    // Check for 401 Unauthorized first - often contains specific messages
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        var errorDto = JsonSerializer.Deserialize<MessageDto>(responseBody); // Backend might return simple message on 401
                        if (!string.IsNullOrWhiteSpace(errorDto?.Message))
                        {
                            // Check for specific messages related to 'RequireConfirmedAccount' if backend provides them
                            if (errorDto.Message.Contains("Account not confirmed", StringComparison.OrdinalIgnoreCase))
                            {
                                errorMessage = "Login failed: Account email needs confirmation.";
                            }
                            else
                            {
                                errorMessage = $"Login failed: {errorDto.Message}"; // Use backend message (e.g., "Invalid password", "Email not found")
                            }
                        }
                        else
                        {
                            errorMessage = "Login failed: Invalid credentials or account issue."; // Generic 401
                        }
                    }
                    // Check for 400 Bad Request (e.g., validation errors, though less common for login)
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        var errorDto = JsonSerializer.Deserialize<ErrorDto>(responseBody);
                        if (errorDto != null)
                        {
                            if (!string.IsNullOrWhiteSpace(errorDto.Message))
                            {
                                errorMessage = errorDto.Message;
                            }
                            if (errorDto.Errors != null && errorDto.Errors.Count > 0)
                            {
                                errorMessage += " Details: " + string.Join("; ", errorDto.Errors);
                            }
                        }
                        else
                        {
                            errorMessage = "Login failed: Invalid request."; // Generic 400
                        }
                    }
                    // Handle other errors (500 Internal Server Error, etc.)
                    else
                    {
                        errorMessage = $"Login failed: Server returned status {response.StatusCode}.";
                    }
                }
                catch (JsonException jsonEx)
                {
                    Logger.Error(jsonEx, "Failed to deserialize error response body.");
                    // Keep the default or status code based error message
                }

                SetStatusMessage(errorMessage, true);
            }
        }
        catch (HttpRequestException httpEx)
        {
            Logger.Error(httpEx, "HTTP request failed during login.");
            SetStatusMessage($"Login failed: Could not connect to the server. ({httpEx.Message})", true);
        }
        catch (JsonException jsonEx) // Error deserializing success or error response
        {
            Logger.Error(jsonEx, "JSON error during login response processing.");
            SetStatusMessage("Login failed: Error processing server response.", true);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An unexpected error occurred during login.");
            SetStatusMessage("An unexpected error occurred during login.", true);
        }
        finally
        {
            // Dispose response message if not null
            response?.Dispose();
        }
    }

    /// <summary>
    /// Handles the click event for the Register button.
    /// </summary>
    private async void HandleRegisterClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Defensive checks for UI elements
        if (_usernameInputBuilder == null || !_usernameInputBuilder.Entity.IsAlive() ||
           _passwordInputBuilder == null || !_passwordInputBuilder.Entity.IsAlive() ||
           _confirmPasswordInputBuilder == null || !_confirmPasswordInputBuilder.Entity.IsAlive())
        {
            SetStatusMessage("Internal error: Input fields not ready.", true);
            Logger.Error("Register attempt failed: UI input builders are null or their entities are dead.");
            return;
        }

        string email = _usernameInputBuilder.GetText()?.Trim() ?? string.Empty;
        string password = _passwordInputBuilder.GetText() ?? string.Empty;
        string confirmPassword = _confirmPasswordInputBuilder.GetText() ?? string.Empty;

        // Basic client-side validation
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            SetStatusMessage("Please fill in all fields.", true);
            return;
        }
        if (!email.Contains('@') || !email.Contains('.')) // Very basic email format check
        {
            SetStatusMessage("Please enter a valid email address.", true);
            return;
        }
        if (password != confirmPassword)
        {
            SetStatusMessage("Passwords do not match.", true);
            return;
        }
        // Consider adding client-side checks for password complexity if desired,
        // although the backend enforces the definitive rules.

        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = confirmPassword
        };

        SetStatusMessage("Registering...", false); // Indicate progress
        HttpResponseMessage? response = null;
        try
        {
            string registerUrl = $"{BackendBaseUrl}/api/auth/register";
            Logger.Info($"Attempting registration to: {registerUrl} for user {email}");

            response = await HttpClient.PostAsJsonAsync(registerUrl, registerDto);
            Logger.Debug($"Registration response status code: {response.StatusCode}");

            if (response.IsSuccessStatusCode) // Status 201 Created
            {
                // Attempt to deserialize success message
                MessageDto? successResponse = await response.Content.ReadFromJsonAsync<MessageDto>();
                string successMsg = successResponse?.Message ?? "Registration successful!";
                Logger.Info($"Registration successful for {email}. Message: {successMsg}");

                SetStatusMessage(successMsg, false);

                // Clear password fields after successful registration
                Dispatcher.UIThread.Post(() =>
                {
                    _passwordInputBuilder?.SetText("");
                    _confirmPasswordInputBuilder?.SetText("");
                });
            }
            else // Handle errors (400 Bad Request, 500 Internal Server Error, etc.)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Logger.Warn($"Registration failed. Status: {response.StatusCode}, Response Body: {errorContent}");

                // Try to parse structured error response
                string errorMessage = "Registration failed. Please check your details."; // Default
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var errorDto = JsonSerializer.Deserialize<ErrorDto>(errorContent, options); // Assumes ErrorDto structure on failure
                    if (errorDto != null)
                    {
                        // Use specific message from backend if provided
                        if (!string.IsNullOrWhiteSpace(errorDto.Message))
                        {
                            errorMessage = errorDto.Message;
                        }
                        // Append validation errors if provided
                        if (errorDto.Errors != null && errorDto.Errors.Count > 0)
                        {
                            errorMessage += " Errors: " + string.Join("; ", errorDto.Errors);
                        }
                    }
                    else
                    {
                        // Try parsing as simple MessageDto if ErrorDto fails
                        var simpleError = JsonSerializer.Deserialize<MessageDto>(errorContent);
                        if (!string.IsNullOrWhiteSpace(simpleError?.Message))
                        {
                            errorMessage = simpleError.Message;
                        }
                    }
                }
                catch (JsonException jsonEx)
                {
                    Logger.Error(jsonEx, "Failed to deserialize registration error response body.");
                    // Stick with default message or use status code
                    errorMessage = $"Registration failed: Server returned status {response.StatusCode}.";
                }

                SetStatusMessage(errorMessage, true);
            }
        }
        catch (HttpRequestException httpEx)
        {
            Logger.Error(httpEx, "HTTP request failed during registration.");
            SetStatusMessage($"Registration failed: Could not connect to the server. ({httpEx.Message})", true);
        }
        catch (JsonException jsonEx) // Error deserializing success or error response
        {
            Logger.Error(jsonEx, "JSON error during registration response processing.");
            SetStatusMessage("Registration failed: Error processing server response.", true);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An unexpected error occurred during registration.");
            SetStatusMessage("An unexpected error occurred during registration.", true);
        }
        finally
        {
            response?.Dispose();
        }
    }

    /// <summary>
    /// Sets the status message text and color on the UI thread.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="isError">True if the message indicates an error (sets text color to red).</param>
    private void SetStatusMessage(string message, bool isError)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Check builder exists and its entity is alive before using
            if (_statusMessageBuilder?.Entity.IsAlive() == true)
            {
                _statusMessageBuilder
                    .SetText(message)
                    .SetForeground(isError ? Brushes.Red : Brushes.ForestGreen); // Use distinct colors
            }
            else
            {
                Logger.Warn($"Status message TextBlock builder is null or its entity is dead. Message: '{message}'");
            }
        });
    }


    /// <inheritdoc/>
    public void Attach(Entity entity)
    {
        if (_root.IsValid() && _root.IsAlive()) { _root.ChildOf(entity); }
    }

    /// <inheritdoc/>
    public void Detach()
    {
        if (_root.IsValid() && _root.IsAlive() && _root.Has(Ecs.ChildOf, Ecs.Wildcard))
        {
            _root.Remove(Ecs.ChildOf, Ecs.Wildcard);
        }
    }
}
