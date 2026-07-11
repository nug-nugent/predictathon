using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.Domain.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Predictathon.Application.Services;

[ScopedService]
public class JwtTokenService : IJwtTokenService
{
    // Short-lived by design - a working refresh flow (IRefreshTokenService/AuthService.RefreshToken)
    // renews this silently, so it doesn't need to survive long if it ever leaks.
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);

    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public AuthResultModel GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var signingKey = jwtSection["SigningKey"];

        if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(signingKey))
        {
            throw new InvalidOperationException("Jwt:Issuer, Jwt:Audience and Jwt:SigningKey must all be configured.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAtUtc = DateTime.UtcNow.Add(AccessTokenLifetime);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new AuthResultModel
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
