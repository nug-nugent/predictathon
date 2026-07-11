using Predictathon.Application.Models;
using Predictathon.Domain.Identity;

namespace Predictathon.Application.Interfaces;

public interface IJwtTokenService
{
    /// <summary>
    /// Builds a signed JWT for the given user and their current roles.
    /// </summary>
    AuthResultModel GenerateToken(ApplicationUser user, IList<string> roles);
}
