using FluentAssertions;
using Predictathon.Application.Services;
using Predictathon.Domain.Identity;
using Predictathon.UnitTests.TestDoubles;
using System.Security.Cryptography;
using System.Text;

namespace Predictathon.UnitTests.Services;

public class RefreshTokenServiceTests
{
    private static (InMemoryApplicationDbContext DbContext, RefreshTokenService Service) MakeService()
    {
        var dbContext = new InMemoryApplicationDbContext();
        var service = new RefreshTokenService(dbContext);
        return (dbContext, service);
    }

    [Fact]
    public async Task GenerateAsync_PersistsHashedTokenNotRawToken()
    {
        var (dbContext, service) = MakeService();
        var userId = Guid.NewGuid();
        var expiresAtUtc = DateTime.UtcNow.AddDays(1);

        var rawToken = await service.GenerateAsync(userId, expiresAtUtc);

        var stored = dbContext.RefreshTokens.Should().ContainSingle().Subject;
        stored.UserId.Should().Be(userId);
        stored.ExpiresAtUtc.Should().Be(expiresAtUtc);
        stored.RevokedAtUtc.Should().BeNull();
        stored.TokenHash.Should().Equal(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        rawToken.Should().NotContain("+").And.NotContain("/").And.NotContain("=");
    }

    [Fact]
    public async Task GenerateAsync_CalledTwice_ProducesDifferentRawTokens()
    {
        var (_, service) = MakeService();

        var first = await service.GenerateAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        var second = await service.GenerateAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(1));

        first.Should().NotBe(second);
    }

    [Fact]
    public async Task ValidateAsync_UnknownToken_ReturnsNull()
    {
        var (_, service) = MakeService();

        var result = await service.ValidateAsync("never-issued");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ActiveToken_ReturnsUserId()
    {
        var (_, service) = MakeService();
        var userId = Guid.NewGuid();
        var rawToken = await service.GenerateAsync(userId, DateTime.UtcNow.AddDays(1));

        var result = await service.ValidateAsync(rawToken);

        result.Should().Be(userId);
    }

    [Fact]
    public async Task ValidateAsync_ExpiredToken_ReturnsNull()
    {
        var (_, service) = MakeService();
        var userId = Guid.NewGuid();
        var rawToken = await service.GenerateAsync(userId, DateTime.UtcNow.AddDays(-1));

        var result = await service.ValidateAsync(rawToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_RevokedToken_ReturnsNull()
    {
        var (_, service) = MakeService();
        var userId = Guid.NewGuid();
        var rawToken = await service.GenerateAsync(userId, DateTime.UtcNow.AddDays(1));
        await service.RevokeAsync(rawToken);

        var result = await service.ValidateAsync(rawToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAsync_ActiveToken_SetsRevokedAtUtc()
    {
        var (dbContext, service) = MakeService();
        var rawToken = await service.GenerateAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(1));

        await service.RevokeAsync(rawToken);

        var stored = dbContext.RefreshTokens.Should().ContainSingle().Subject;
        stored.RevokedAtUtc.Should().NotBeNull();
        stored.RevokedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RevokeAsync_AlreadyRevokedToken_IsNoOp()
    {
        var (dbContext, service) = MakeService();
        var rawToken = await service.GenerateAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        await service.RevokeAsync(rawToken);
        var firstRevokedAt = dbContext.RefreshTokens.Single().RevokedAtUtc;

        await service.RevokeAsync(rawToken);

        dbContext.RefreshTokens.Single().RevokedAtUtc.Should().Be(firstRevokedAt);
    }

    [Fact]
    public async Task RevokeAsync_UnknownToken_DoesNotThrow()
    {
        var (_, service) = MakeService();

        var act = () => service.RevokeAsync("never-issued");

        await act.Should().NotThrowAsync();
    }
}
