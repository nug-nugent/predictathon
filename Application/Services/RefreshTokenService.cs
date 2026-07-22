using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Domain.Identity;
using System.Security.Cryptography;
using System.Text;

namespace Predictathon.Application.Services;

[ScopedService]
public class RefreshTokenService : IRefreshTokenService
{
    private readonly IGenericDbContext _dbContext;

    public RefreshTokenService(IGenericDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(Guid userId, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        var rawToken = GenerateRawToken();

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return rawToken;
    }

    /// <inheritdoc />
    public async Task<Guid?> ValidateAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(rawToken);
        var entity = await _dbContext.FirstOrDefaultAsync<RefreshToken>(t => t.TokenHash == hash, cancellationToken);

        return entity is not null && entity.IsActive ? entity.UserId : null;
    }

    /// <inheritdoc />
    public async Task RevokeAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(rawToken);
        var entity = await _dbContext.FirstOrDefaultAsync<RefreshToken>(t => t.TokenHash == hash, cancellationToken);

        if (entity is null || entity.RevokedAtUtc is not null)
        {
            return;
        }

        entity.RevokedAtUtc = DateTime.UtcNow;
        _dbContext.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        // base64url, no padding - safe to use as a cookie value without further encoding.
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static byte[] Hash(string rawToken)
        => SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
}
