using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Predictathon.Application.Errors;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.Application.Services;
using Predictathon.Domain.Identity;
using Predictathon.UnitTests.TestDoubles;

namespace Predictathon.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager = MockUserManager.Create();
    private readonly Mock<IJwtTokenService> _tokenService = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenService = new();
    private readonly Mock<IAvatarService> _avatarService = new();
    private readonly Mock<IEmailService> _emailService = new();

    private AuthService MakeService()
        => new(
            _userManager.Object,
            _tokenService.Object,
            _refreshTokenService.Object,
            _avatarService.Object,
            _emailService.Object,
            new ConfigurationBuilder().Build());

    private static ApplicationUser MakeUser(int accessFailedCount = 0) => new()
    {
        Id = Guid.NewGuid(),
        UserName = "someone",
        Email = "someone@example.com",
        AccessFailedCount = accessFailedCount,
    };

    private void SetupFoundUser(ApplicationUser user, bool isLockedOut = false)
    {
        _userManager.Setup(m => m.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(isLockedOut);
        _userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(true);
    }

    [Fact]
    public async Task Login_WrongPassword_RecordsAccessFailureAndReturnsGenericError()
    {
        var user = MakeUser();
        SetupFoundUser(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);
        _userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await MakeService().Login(new LoginModel { UserName = user.UserName!, Password = "wrong" });

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.Message == "Invalid username or password.");
        _userManager.Verify(m => m.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task Login_WrongPasswordTripsLockoutThreshold_ReturnsLockedOutMessage()
    {
        var user = MakeUser();
        SetupFoundUser(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);
        _userManager.Setup(m => m.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success)
            .Callback(() => _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true));

        var result = await MakeService().Login(new LoginModel { UserName = user.UserName!, Password = "wrong" });

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.Message == "Too many failed login attempts. Please try again later.");
    }

    [Fact]
    public async Task Login_CorrectPasswordAfterPriorFailures_ResetsAccessFailedCount()
    {
        var user = MakeUser(accessFailedCount: 2);
        SetupFoundUser(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);
        _tokenService.Setup(t => t.GenerateToken(user, It.IsAny<IList<string>>())).Returns(new AuthResultModel());
        _refreshTokenService.Setup(r => r.GenerateAsync(user.Id, It.IsAny<DateTime>(), default)).ReturnsAsync("refresh-token");

        var result = await MakeService().Login(new LoginModel { UserName = user.UserName!, Password = "correct" });

        result.IsSuccess.Should().BeTrue();
        _userManager.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task Login_CorrectPasswordWithNoPriorFailures_DoesNotResetAccessFailedCount()
    {
        var user = MakeUser(accessFailedCount: 0);
        SetupFoundUser(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);
        _tokenService.Setup(t => t.GenerateToken(user, It.IsAny<IList<string>>())).Returns(new AuthResultModel());
        _refreshTokenService.Setup(r => r.GenerateAsync(user.Id, It.IsAny<DateTime>(), default)).ReturnsAsync("refresh-token");

        var result = await MakeService().Login(new LoginModel { UserName = user.UserName!, Password = "correct" });

        result.IsSuccess.Should().BeTrue();
        _userManager.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Never);
    }
}
