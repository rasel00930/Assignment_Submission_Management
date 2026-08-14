using System.Net;
using System.Net.Mail;
using AssignmentManagement.Core.Exceptions;
using AssignmentManagement.Service.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AssignmentManagement.Service.Implementation;

public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendAccountCredentialsAsync(
        string recipientEmail,
        string recipientName,
        string userName,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        var loginUrl = BuildFrontendUrl("/login");
        var body = $"""
            <h2>Welcome to AssignmentHub</h2>
            <p>Hello {Encode(recipientName)},</p>
            <p>Your account has been created. Use these temporary credentials to sign in:</p>
            <p><strong>Username:</strong> {Encode(userName)}<br/>
            <strong>Temporary password:</strong> {Encode(temporaryPassword)}</p>
            <p><a href="{Encode(loginUrl)}">Sign in to AssignmentHub</a></p>
            <p>For your security, change the temporary password after signing in.</p>
            """;

        return SendAsync(recipientEmail, "Your AssignmentHub account", body, cancellationToken);
    }

    public Task SendPasswordResetCodeAsync(
        string recipientEmail,
        string recipientName,
        string verificationCode,
        int expiresInMinutes,
        CancellationToken cancellationToken = default)
    {
        var resetUrl = BuildFrontendUrl($"/forgot-password?email={Uri.EscapeDataString(recipientEmail)}");
        var body = $"""
            <h2>Password reset verification</h2>
            <p>Hello {Encode(recipientName)},</p>
            <p>Your AssignmentHub password reset verification code is:</p>
            <p style="font-size:28px;font-weight:700;letter-spacing:6px">{Encode(verificationCode)}</p>
            <p>This code expires in {expiresInMinutes} minutes and can be used only once.</p>
            <p><a href="{Encode(resetUrl)}">Continue password reset</a></p>
            <p>If you did not request this reset, you can safely ignore this email.</p>
            """;

        return SendAsync(recipientEmail, "AssignmentHub password reset code", body, cancellationToken);
    }

    private async Task SendAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(recipientEmail));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = string.IsNullOrWhiteSpace(_options.UserName)
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
            client.Credentials = new NetworkCredential(_options.UserName, _options.Password);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException)
        {
            _logger.LogError(exception, "SMTP delivery to {RecipientEmail} failed.", recipientEmail);
            throw new AppException(503, "Email could not be delivered. Check the SMTP configuration.");
        }
    }

    private void EnsureConfigured()
    {
        if (!_options.Enabled ||
            string.IsNullOrWhiteSpace(_options.Host) ||
            string.IsNullOrWhiteSpace(_options.FromEmail) ||
            _options.Port is < 1 or > 65535 ||
            !MailAddress.TryCreate(_options.FromEmail, out _))
            throw new AppException(503, "Email delivery is not configured. Configure the Email settings first.");
    }

    private string BuildFrontendUrl(string path) =>
        $"{_options.FrontendBaseUrl.TrimEnd('/')}{path}";

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
