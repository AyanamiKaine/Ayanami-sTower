# Email Status Service for .NET

AI GENERATED README

A simple, reusable C#/.NET utility for sending richly formatted HTML status updates via email. It's designed to be easily integrated into server applications, deployment scripts, or any backend process to provide notifications on successes, warnings, or errors.

The service is configured entirely through environment variables for security and flexibility.

## ✨ Features

* **Rich HTML Emails**: Sends visually clear emails with status-colored headers and borders.
* **Status Levels**: Supports multiple notification levels (`Info`, `Success`, `Warning`, `Error`).
* **Environment-Based Configuration**: No hardcoded credentials. All settings are read from environment variables.
* **Automatic Context**: Automatically includes the server name, application name, and a timestamp in every email for easy debugging.
* **Convenience Methods**: Simple one-liners for sending different types of status updates (e.g., `SendSuccessAsync`).
* **Batching Support**: Includes a `StatusBatch` helper class to collect multiple messages and send them as a single, consolidated email report.

## ⚙️ Configuration

To use the `EmailStatusService`, you must set the following environment variables on the machine where the application is running:

| Variable              | Required | Description                                                               | Default Value    |
| --------------------- | :------: | ------------------------------------------------------------------------- | ---------------- |
| `EMAIL_FROM`          |   Yes    | The email address that will send the notifications.                       | `(none)`         |
| `EMAIL_PASSWORD`      |   Yes    | The password or app-specific password for the `EMAIL_FROM` account.       | `(none)`         |
| `EMAIL_SMTP_SERVER`   |    No    | The SMTP server address for your email provider.                          | `"smtp.gmail.com"` |
| `EMAIL_SMTP_PORT`     |    No    | The port for the SMTP server.                                             | `"587"`          |
| `EMAIL_TO`            |    No    | The recipient's email address.                                            | `EMAIL_FROM`     |

## 🚀 Usage

Since you enjoy code examples, here are a couple of ways you can use the service.

### Example 1: Sending a Single Error Notification

This is useful for immediately reporting a critical failure.

```csharp
// 1. Instantiate the service. It will automatically load config from environment variables.
var emailService = new EmailStatusService();

// 2. Define your message
string subject = "Deployment Failed";
string message = "The deployment script encountered a fatal error when migrating the database. Please check the logs immediately.";

// 3. Send the email
try
{
    await emailService.SendErrorAsync(subject, message);
    Console.WriteLine("Error notification sent.");
}
catch (Exception ex)
{
    // This could happen if environment variables are not set
    Console.WriteLine($"Could not instantiate email service: {ex.Message}");
}
```

### Example 2: Sending a Batched Report

This is perfect for collecting results from a multi-step process and sending a single summary email at the end.

```csharp
var emailService = new EmailStatusService();
var batch = new StatusBatch(emailService);

// --- Start of a long process ---

// Step 1 succeeded
batch.AddStatus("Database connection successful.", StatusLevel.Success);

// Step 2 has a non-critical issue
batch.AddStatus("Could not clear temporary cache folder. Manual cleanup may be required.", StatusLevel.Warning);

// Step 3 succeeded
batch.AddStatus("Application binaries successfully copied to destination.", StatusLevel.Success);

// --- End of the process ---

// Send the consolidated report. The email will be marked with the highest
// status level encountered (in this case, Warning).
await batch.SendBatchAsync("Nightly Build Process Summary");
Console.WriteLine("Batch report has been sent.");
```