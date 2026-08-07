using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Predictathon.Domain.Identity;

namespace Predictathon.UnitTests.TestDoubles;

/// <summary>
/// Builds a mockable <see cref="UserManager{TUser}"/> for <see cref="ApplicationUser"/>. UserManager
/// has no interface, so tests that need to control its behaviour (e.g. AuthService) must Moq the
/// concrete class directly; the backing store is a no-op since only UserManager's own virtual
/// methods get stubbed per-test.
/// </summary>
public static class MockUserManager
{
    public static Mock<UserManager<ApplicationUser>> Create()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            (IOptions<IdentityOptions>)null!,
            (IPasswordHasher<ApplicationUser>)null!,
            (IEnumerable<IUserValidator<ApplicationUser>>)null!,
            (IEnumerable<IPasswordValidator<ApplicationUser>>)null!,
            (ILookupNormalizer)null!,
            (IdentityErrorDescriber)null!,
            (IServiceProvider)null!,
            (ILogger<UserManager<ApplicationUser>>)null!);
    }
}
