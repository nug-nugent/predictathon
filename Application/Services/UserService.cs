using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;

namespace Predictathon.Application.Services;

[ScopedService]
public class UserService : IUserService
{
    private readonly IApplicationDbContext _dbContext;

    public UserService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfileModel?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.User.FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return new UserProfileModel
        {
            UserID = user.UserID,
            Username = user.Username,
            Caption = user.Caption,
            Location = user.Location,
            FavouriteTeam = user.FavouriteTeam,
            ProfileText = user.ProfileText,
        };
    }
}
