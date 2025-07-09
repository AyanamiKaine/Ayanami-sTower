using System.Net;
using System.Net.Mail;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace AyanamisTower.Email;

/// <summary>
/// Mainly used for my personal server to send me information to my email.
/// </summary>
public class EmailStatusService
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _fromEmail;
    private readonly string _fromPassword;
    private readonly string _toEmail;

    public EmailStatusService()
    {
        // Load from environment variables
        _smtpServer = Environment.GetEnvironmentVariable("EMAIL_SMTP_SERVER") ?? "smtp.gmail.com";
        _smtpPort = int.Parse(Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT") ?? "587");
        _fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM") ??
            throw new InvalidOperationException("EMAIL_FROM environment variable is required");
        _fromPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ??
            throw new InvalidOperationException("EMAIL_PASSWORD environment variable is required");
        _toEmail = Environment.GetEnvironmentVariable("EMAIL_TO") ?? _fromEmail;
    }

    public async Task SendStatusUpdateAsync(string subject, string message, StatusLevel level = StatusLevel.Info)
    {
        try
        {
            using var smtpClient = new SmtpClient(_smtpServer, _smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_fromEmail, _fromPassword)
            };

            var mailMessage = new MailMessage(_fromEmail, _toEmail)
            {
                Subject = $"[{level.ToString().ToUpper()}] {subject}",
                Body = FormatMessage(message, level),
                IsBodyHtml = true
            };

            await smtpClient.SendMailAsync(mailMessage);
            Console.WriteLine($"Status email sent successfully: {subject}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
            // Consider logging to file or other notification method as fallback
        }
    }

    private string FormatMessage(string message, StatusLevel level)
    {
        var color = level switch
        {
            StatusLevel.Success => "#28a745",
            StatusLevel.Warning => "#ffc107",
            StatusLevel.Error => "#dc3545",
            StatusLevel.Info => "#17a2b8",
            _ => "#6c757d"
        };

        return $@"
        <html>
        <body style='font-family: Arial, sans-serif;'>
            <div style='padding: 20px; border-left: 4px solid {color};'>
                <h3 style='color: {color}; margin: 0 0 10px 0;'>{level} Status Update</h3>
                <p style='margin: 0; color: #333;'>{message}</p>
                <hr style='margin: 20px 0; border: none; border-top: 1px solid #eee;'>
                <small style='color: #666;'>
                    Server: {Environment.MachineName}<br>
                    Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}<br>
                    Application: {System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name}
                </small>
            </div>
        </body>
        </html>";
    }

    // Convenience methods for different status levels
    public Task SendSuccessAsync(string subject, string message) =>
        SendStatusUpdateAsync(subject, message, StatusLevel.Success);

    public Task SendWarningAsync(string subject, string message) =>
        SendStatusUpdateAsync(subject, message, StatusLevel.Warning);

    public Task SendErrorAsync(string subject, string message) =>
        SendStatusUpdateAsync(subject, message, StatusLevel.Error);

    public Task SendInfoAsync(string subject, string message) =>
        SendStatusUpdateAsync(subject, message, StatusLevel.Info);
}

public enum StatusLevel
{
    Info,
    Success,
    Warning,
    Error
}

// Helper class for batch status updates
public class StatusBatch
{
    private readonly EmailStatusService _emailService;
    private readonly List<string> _messages = new();
    private StatusLevel _highestLevel = StatusLevel.Info;

    public StatusBatch(EmailStatusService emailService)
    {
        _emailService = emailService;
    }

    public void AddStatus(string message, StatusLevel level = StatusLevel.Info)
    {
        _messages.Add($"[{level}] {message}");
        if (level > _highestLevel)
            _highestLevel = level;
    }

    public async Task SendBatchAsync(string subject)
    {
        if (_messages.Count == 0) return;

        var combinedMessage = string.Join("<br>", _messages);
        await _emailService.SendStatusUpdateAsync(subject, combinedMessage, _highestLevel);
        _messages.Clear();
        _highestLevel = StatusLevel.Info;
    }
}