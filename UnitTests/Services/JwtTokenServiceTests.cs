using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Predictathon.Application.Services;
using Predictathon.Domain.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Predictathon.UnitTests.Services;

public class JwtTokenServiceTests
{
    private const string Issuer = "predictathon-tests";
    private const string Audience = "predictathon-tests-audience";
    private const string SigningKey = "this-is-a-sufficiently-long-test-signing-key-0123456789";

    private static JwtTokenService MakeService(IDictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:SigningKey"] = SigningKey,
        };

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                settings[key] = value;
            }
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new JwtTokenService(configuration);
    }

    private static ApplicationUser MakeUser() => new()
    {
        Id = Guid.NewGuid(),
        UserName = "someone",
        Email = "someone@example.com",
    };

    [Theory]
    [InlineData("Jwt:Issuer")]
    [InlineData("Jwt:Audience")]
    [InlineData("Jwt:SigningKey")]
    public void GenerateToken_MissingRequiredConfig_Throws(string missingKey)
    {
        var service = MakeService(new Dictionary<string, string?> { [missingKey] = null });

        var act = () => service.GenerateToken(MakeUser(), []);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GenerateToken_ValidConfig_ProducesParseableTokenWithExpectedClaims()
    {
        var service = MakeService();
        var user = MakeUser();

        var result = service.GenerateToken(user, ["MatchAdministrator", "UserAdministrator"]);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        token.Issuer.Should().Be(Issuer);
        token.Audiences.Should().Contain(Audience);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.UserName);
        token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
            .Should().BeEquivalentTo(["MatchAdministrator", "UserAdministrator"]);
    }

    [Fact]
    public void GenerateToken_NoRoles_ProducesTokenWithNoRoleClaims()
    {
        var service = MakeService();

        var result = service.GenerateToken(MakeUser(), []);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        token.Claims.Should().NotContain(c => c.Type == ClaimTypes.Role);
    }

    [Fact]
    public void GenerateToken_SetsExpiryAroundFifteenMinutesFromNow()
    {
        var service = MakeService();

        var result = service.GenerateToken(MakeUser(), []);

        result.ExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateToken_CalledTwice_ProducesDifferentJtiClaims()
    {
        var service = MakeService();
        var user = MakeUser();

        var first = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateToken(user, []).Token);
        var second = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateToken(user, []).Token);

        var firstJti = first.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var secondJti = second.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        firstJti.Should().NotBe(secondJti);
    }
}
