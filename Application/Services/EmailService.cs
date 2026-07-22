using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using System.Net;
using System.Net.Mail;

namespace Predictathon.Application.Services;

[ScopedService]
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"];

        // No SMTP host configured (local dev by default) - log instead of sending, so
        // password-reset and similar flows stay testable without a real mail server and without
        // ever risking a real email going to a real address from a dev box.
        if (string.IsNullOrEmpty(host))
        {
            _logger.LogInformation(
                "Smtp:Host not configured - logging email instead of sending.\nTo: {ToEmail}\nSubject: {Subject}\n{Body}",
                toEmail, subject, htmlBody);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_configuration["Smtp:FromAddress"] ?? "noreply@predictathon.co.uk", _configuration["Smtp:FromName"] ?? "Predictathon"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(host, int.TryParse(_configuration["Smtp:Port"], out var port) ? port : 587)
        {
            EnableSsl = !bool.TryParse(_configuration["Smtp:EnableSsl"], out var enableSsl) || enableSsl
        };

        var username = _configuration["Smtp:Username"];
        if (!string.IsNullOrEmpty(username))
        {
            client.Credentials = new NetworkCredential(username, _configuration["Smtp:Password"]);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
