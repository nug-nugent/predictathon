using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Predictathon.Application.Services;

namespace Predictathon.UnitTests.Services;

public class EmailServiceTests
{
    [Fact]
    public async Task SendAsync_NoSmtpHostConfigured_CompletesWithoutSendingOrThrowing()
    {
        var service = new EmailService(new ConfigurationBuilder().Build(), NullLogger<EmailService>.Instance);

        var act = () => service.SendAsync("someone@example.com", "Subject", "<p>Body</p>");

        await act.Should().NotThrowAsync();
    }
}
