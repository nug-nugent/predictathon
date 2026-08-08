using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Predictathon.Application.Attributes;
using Predictathon.Application.Constants;
using Predictathon.Application.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;

namespace Predictathon.Application.Services;

[ScopedService]
public class EmailService : IEmailService
{
    private const string LogoContentId = "predictathon-logo";
    private const string LogoResourceName = "Predictathon.Application.Resources.EmailLogo.png";

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
        };
        message.To.Add(toEmail);

        using var logoResource = BuildLogoResource();
        var htmlView = AlternateView.CreateAlternateViewFromString(BuildHtmlDocument(htmlBody), null, "text/html");
        htmlView.LinkedResources.Add(logoResource);
        message.AlternateViews.Add(htmlView);

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

    /// <summary>
    /// Loads the embedded Predictathon wordmark image as a CID-linked resource, so it renders
    /// inline in the header without depending on a remote image being fetched (and without the
    /// patchy client support for base64 data-URI images).
    /// </summary>
    private static LinkedResource BuildLogoResource()
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{LogoResourceName}' not found.");

        return new LinkedResource(stream, "image/png")
        {
            ContentId = LogoContentId,
            TransferEncoding = TransferEncoding.Base64
        };
    }

    /// <summary>
    /// Wraps a bare HTML content fragment in Predictathon's shared branded email shell: a
    /// table-based layout (for compatibility with older email clients, notably desktop Outlook)
    /// with a header carrying the logo/wordmark and a muted footer disclaimer.
    /// </summary>
    /// <param name="bodyContentHtml">The inner content - the same plain HTML fragments the callers already built.</param>
    private static string BuildHtmlDocument(string bodyContentHtml)
    {
        var linkStyle = $"a {{ color: {EmailStyle.HeaderBlue}; text-decoration: none; font-weight: 600; }}";

        return $"""
        <!doctype html>
        <html>
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <style>{linkStyle}</style>
        </head>
        <body style="margin:0;padding:24px 16px;background-color:#F0F2F7;font-family:Arial,Helvetica,sans-serif;">
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"><tr><td align="center">
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;background-color:#FFFFFF;border-radius:10px;overflow:hidden;">
        <tr><td style="background-color:{EmailStyle.HeaderBlue};padding:20px 28px;">
        <table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr>
        <td style="vertical-align:middle;padding-right:10px;"><img src="cid:{LogoContentId}" width="28" height="28" alt="" style="display:block;border:0;"></td>
        <td style="vertical-align:middle;font-family:Arial,Helvetica,sans-serif;font-size:19px;font-weight:800;letter-spacing:0.05em;color:#FFFFFF;text-transform:uppercase;">Predictathon</td>
        </tr></table>
        </td></tr>
        <tr><td style="padding:32px 28px;font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:1.6;color:{EmailStyle.BodyInk};">
        {bodyContentHtml}
        </td></tr>
        <tr><td style="padding:16px 28px;border-top:1px solid {EmailStyle.FooterBorder};background-color:{EmailStyle.FooterBg};font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:1.5;color:{EmailStyle.FooterInk};text-align:center;">
        This is an automated email from Predictathon; you're receiving it because this address is on your account.
        </td></tr>
        </table>
        </td></tr></table>
        </body>
        </html>
        """;
    }
}
