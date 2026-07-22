namespace Predictathon.Application.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends an HTML email. In environments without an <c>Smtp:Host</c> configured, implementations
    /// should log the email instead of sending it, so flows that depend on it stay testable locally.
    /// </summary>
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
