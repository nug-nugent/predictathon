using Microsoft.AspNetCore.Identity;
using Predictathon.Application.Constants;

namespace Predictathon.WebApi.Extensions;

public static class IdentityExtensions
{
    private static readonly string[] Roles =
    [
        RoleConstants.MatchAdministrator,
        RoleConstants.UserAdministrator,
        RoleConstants.CompetitionAdministrator
    ];

    /// <summary>
    /// Ensures the roles in <see cref="RoleConstants"/> exist, so [Authorize(Roles = ...)] has
    /// something to check against without manual database setup.
    /// </summary>
    public static async Task SeedRolesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }
    }
}
