using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Predictathon.Application.Constants;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Services;
using Predictathon.Domain.Identity;
using Predictathon.UnitTests.TestDoubles;

namespace Predictathon.UnitTests.Services;

public class UserServiceTests
{
    private readonly InMemoryApplicationDbContext _dbContext = new();
    private readonly Mock<IAvatarService> _avatarService = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManager = MockUserManager.Create();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<ITrophyService> _trophyService = new();

    public UserServiceTests()
    {
        // Trophies are their own feature with their own tests - nobody here has won anything.
        _trophyService.Setup(t => t.GetForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    private UserService MakeService()
        => new(
            _avatarService.Object,
            _userManager.Object,
            _dbContext,
            _emailService.Object,
            _trophyService.Object,
            new ConfigurationBuilder().Build(),
            NullLogger<UserService>.Instance);

    private static ApplicationUser MakeUser() => new() { Id = Guid.NewGuid(), UserName = "someone" };

    [Fact]
    public async Task UpdateUserRolesAsync_UnknownRole_ReturnsValidationFailure()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var result = await MakeService().UpdateUserRolesAsync(user.Id, ["NotARole"], Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.Message.Contains("NotARole"));
        _userManager.Verify(m => m.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_UnknownUser_ReturnsNotFound()
    {
        _userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await MakeService().UpdateUserRolesAsync(Guid.NewGuid(), [RoleConstants.MatchAdministrator], Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_SelfRemovingUserAdministratorRole_IsRejected()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([RoleConstants.UserAdministrator]);

        var result = await MakeService().UpdateUserRolesAsync(user.Id, [], currentUserId: user.Id);

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.Message == "You cannot remove your own UserAdministrator role.");
        _userManager.Verify(m => m.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_OtherAdminRemovingSomeoneElsesUserAdministratorRole_IsAllowed()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([RoleConstants.UserAdministrator]);
        _userManager.Setup(m => m.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.Contains(RoleConstants.UserAdministrator))))
            .ReturnsAsync(IdentityResult.Success);

        var result = await MakeService().UpdateUserRolesAsync(user.Id, [], currentUserId: Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        _userManager.Verify(m => m.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.Contains(RoleConstants.UserAdministrator))), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_AddsAndRemovesOnlyTheDiff()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([RoleConstants.MatchAdministrator, RoleConstants.CompetitionAdministrator]);
        _userManager.Setup(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);

        var result = await MakeService().UpdateUserRolesAsync(
            user.Id,
            [RoleConstants.MatchAdministrator, RoleConstants.UserAdministrator],
            currentUserId: Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        _userManager.Verify(m => m.AddToRolesAsync(user, It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { RoleConstants.UserAdministrator }))), Times.Once);
        _userManager.Verify(m => m.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.SequenceEqual(new[] { RoleConstants.CompetitionAdministrator }))), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_AddToRolesFails_ReturnsIdentityErrors()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);
        _userManager.Setup(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "Boom", Description = "Boom." }));

        var result = await MakeService().UpdateUserRolesAsync(user.Id, [RoleConstants.MatchAdministrator], Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.Message == "Boom.");
    }

    [Fact]
    public async Task SetUserLockedAsync_LockingOwnAccount_IsRejected()
    {
        var userId = Guid.NewGuid();

        var result = await MakeService().SetUserLockedAsync(userId, locked: true, currentUserId: userId);

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.Message == "You cannot lock your own account.");
        _userManager.Verify(m => m.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), It.IsAny<DateTimeOffset?>()), Times.Never);
    }

    [Fact]
    public async Task SetUserLockedAsync_UnlockingOwnAccount_IsAllowed()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);

        var result = await MakeService().SetUserLockedAsync(user.Id, locked: false, currentUserId: user.Id);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SetUserLockedAsync_UnknownUser_ReturnsNotFound()
    {
        _userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await MakeService().SetUserLockedAsync(Guid.NewGuid(), locked: true, currentUserId: Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task SetUserLockedAsync_LockingAnotherUser_SetsLockoutEndToMaxValue()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue)).ReturnsAsync(IdentityResult.Success);

        var result = await MakeService().SetUserLockedAsync(user.Id, locked: true, currentUserId: Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        _userManager.Verify(m => m.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue), Times.Once);
    }

    [Fact]
    public async Task SetUserLockedAsync_UnlockingAnotherUser_ClearsLockoutEnd()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);

        var result = await MakeService().SetUserLockedAsync(user.Id, locked: false, currentUserId: Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        _userManager.Verify(m => m.SetLockoutEndDateAsync(user, null), Times.Once);
    }

    [Fact]
    public async Task SetUserLockedAsync_IdentityFailure_ReturnsIdentityErrors()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "Boom", Description = "Boom." }));

        var result = await MakeService().SetUserLockedAsync(user.Id, locked: true, currentUserId: Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.Message == "Boom.");
    }
}
