using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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
            new ConfigurationBuilder().Build(),
            NullLogger<AuthService>.Instance);

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

    [Fact]
    public async Task Login_UnknownUsername_ReturnsGenericErrorWithoutTouchingLockoutMachinery()
    {
        _userManager.Setup(m => m.FindByNameAsync("nobody")).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.FindByEmailAsync("nobody")).ReturnsAsync((ApplicationUser?)null);

        var result = await MakeService().Login(new LoginModel { UserName = "nobody", Password = "whatever" });

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.Message == "Invalid username or password.");
        _userManager.Verify(m => m.AccessFailedAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Login_UserHasNoPassword_ReturnsPasswordResetRequiredError()
    {
        var user = MakeUser();
        SetupFoundUser(user);
        _userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(false);

        var result = await MakeService().Login(new LoginModel { UserName = user.UserName!, Password = "anything" });

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is PasswordResetRequiredError);
    }

    [Fact]
    public async Task Login_CorrectPassword_IssuesTokensAndRefreshToken()
    {
        var user = MakeUser();
        SetupFoundUser(user);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["MatchAdministrator"]);
        var tokenResponse = new AuthResultModel { Token = "jwt", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15) };
        _tokenService.Setup(t => t.GenerateToken(user, It.Is<IList<string>>(r => r.Contains("MatchAdministrator")))).Returns(tokenResponse);
        _refreshTokenService.Setup(r => r.GenerateAsync(user.Id, It.IsAny<DateTime>(), default)).ReturnsAsync("refresh-token");
        _avatarService.Setup(a => a.GetAvatarUrl(user.Id, user.ImageUploaded)).Returns("avatar.png");

        var result = await MakeService().Login(new LoginModel { UserName = user.UserName!, Password = "correct" });

        result.IsSuccess.Should().BeTrue();
        result.Value.Response.Token.Should().Be("jwt");
        result.Value.Response.AvatarUrl.Should().Be("avatar.png");
        result.Value.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task Register_CreateFails_ReturnsIdentityErrorsWithoutIssuingTokens()
    {
        var model = new RegisterModel { UserName = "someone", Email = "someone@example.com", Password = "pw" };
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Username already taken." }));

        var result = await MakeService().Register(model);

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.Message == "Username already taken.");
        _refreshTokenService.Verify(r => r.GenerateAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), default), Times.Never);
    }

    [Fact]
    public async Task Register_CreateSucceeds_IssuesTokens()
    {
        var model = new RegisterModel { UserName = "someone", Email = "someone@example.com", Password = "pw", RememberMe = true };
        ApplicationUser? createdUser = null;
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
            .Callback<ApplicationUser, string>((u, _) => createdUser = u)
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync([]);
        _tokenService.Setup(t => t.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>())).Returns(new AuthResultModel { Token = "jwt" });
        _refreshTokenService.Setup(r => r.GenerateAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), default)).ReturnsAsync("refresh-token");

        var result = await MakeService().Register(model);

        result.IsSuccess.Should().BeTrue();
        result.Value.Response.Token.Should().Be("jwt");
        createdUser.Should().NotBeNull();
        createdUser!.UserName.Should().Be(model.UserName);
        createdUser.Email.Should().Be(model.Email);
    }

    [Fact]
    public async Task Register_CreateSucceeds_SendsWelcomeEmail()
    {
        var model = new RegisterModel { UserName = "someone", Email = "someone@example.com", Password = "pw", Forenames = "Dave" };
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), model.Password)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync([]);
        _tokenService.Setup(t => t.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>())).Returns(new AuthResultModel { Token = "jwt" });
        _refreshTokenService.Setup(r => r.GenerateAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), default)).ReturnsAsync("refresh-token");

        await MakeService().Register(model);

        _emailService.Verify(e => e.SendAsync(
            model.Email,
            It.IsAny<string>(),
            It.Is<string>(body => body.Contains(model.Forenames) && body.Contains(model.UserName)),
            default), Times.Once);
    }

    [Fact]
    public async Task Register_WelcomeEmailSendFails_StillReturnsSuccess()
    {
        var model = new RegisterModel { UserName = "someone", Email = "someone@example.com", Password = "pw", Forenames = "Dave" };
        _userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), model.Password)).ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync([]);
        _tokenService.Setup(t => t.GenerateToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>())).Returns(new AuthResultModel { Token = "jwt" });
        _refreshTokenService.Setup(r => r.GenerateAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), default)).ReturnsAsync("refresh-token");
        _emailService.Setup(e => e.SendAsync(model.Email, It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new InvalidOperationException("SMTP unavailable"));

        var result = await MakeService().Register(model);

        result.IsSuccess.Should().BeTrue();
        result.Value.Response.Token.Should().Be("jwt");
    }

    [Fact]
    public async Task RefreshToken_NoTokenSupplied_ReturnsUnauthorized()
    {
        var result = await MakeService().RefreshToken(null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is UnauthorizedError);
    }

    [Fact]
    public async Task RefreshToken_InvalidOrExpiredToken_ReturnsUnauthorized()
    {
        _refreshTokenService.Setup(r => r.ValidateAsync("bad-token", default)).ReturnsAsync((Guid?)null);

        var result = await MakeService().RefreshToken("bad-token");

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is UnauthorizedError);
    }

    [Fact]
    public async Task RefreshToken_UserNoLongerExists_ReturnsUnauthorized()
    {
        var userId = Guid.NewGuid();
        _refreshTokenService.Setup(r => r.ValidateAsync("good-token", default)).ReturnsAsync(userId);
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

        var result = await MakeService().RefreshToken("good-token");

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is UnauthorizedError);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewAccessToken()
    {
        var user = MakeUser();
        _refreshTokenService.Setup(r => r.ValidateAsync("good-token", default)).ReturnsAsync(user.Id);
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync([]);
        _tokenService.Setup(t => t.GenerateToken(user, It.IsAny<IList<string>>())).Returns(new AuthResultModel { Token = "new-jwt" });
        _avatarService.Setup(a => a.GetAvatarUrl(user.Id, user.ImageUploaded)).Returns((string?)null);

        var result = await MakeService().RefreshToken("good-token");

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("new-jwt");
    }

    [Fact]
    public async Task Logout_WithToken_RevokesIt()
    {
        await MakeService().Logout("some-token");

        _refreshTokenService.Verify(r => r.RevokeAsync("some-token", default), Times.Once);
    }

    [Fact]
    public async Task Logout_NoToken_DoesNotCallRevoke()
    {
        await MakeService().Logout(null);

        _refreshTokenService.Verify(r => r.RevokeAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_UnknownUser_StillSucceedsWithoutSendingEmail()
    {
        _userManager.Setup(m => m.FindByNameAsync("nobody")).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.FindByEmailAsync("nobody")).ReturnsAsync((ApplicationUser?)null);

        var result = await MakeService().ForgotPassword(new ForgotPasswordModel { UserNameOrEmail = "nobody" });

        result.IsSuccess.Should().BeTrue();
        _emailService.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_KnownUser_SendsResetEmail()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");

        var result = await MakeService().ForgotPassword(new ForgotPasswordModel { UserNameOrEmail = user.UserName! });

        result.IsSuccess.Should().BeTrue();
        _emailService.Verify(e => e.SendAsync(user.Email!, It.IsAny<string>(), It.Is<string>(body => body.Contains("reset-token")), default), Times.Once);
    }

    [Fact]
    public async Task AdminResetPasswordAsync_UnknownUser_ReturnsNotFound()
    {
        _userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await MakeService().AdminResetPasswordAsync(Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e is NotFoundError);
    }

    [Fact]
    public async Task AdminResetPasswordAsync_KnownUser_SendsResetEmail()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");

        var result = await MakeService().AdminResetPasswordAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        _emailService.Verify(e => e.SendAsync(user.Email!, It.IsAny<string>(), It.Is<string>(body => body.Contains("reset-token")), default), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_UnknownUser_ReturnsFailure()
    {
        var result = await MakeService().ResetPassword(new ResetPasswordModel { UserId = Guid.NewGuid(), Token = "t", NewPassword = "newpw" });

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.Message == "This password reset link is invalid or has expired.");
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsIdentityErrors()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.ResetPasswordAsync(user, "bad-token", "newpw"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var result = await MakeService().ResetPassword(new ResetPasswordModel { UserId = user.Id, Token = "bad-token", NewPassword = "newpw" });

        result.IsFailed.Should().BeTrue();
        result.Errors.OfType<PropertyValidationError>().Should().ContainSingle(e => e.Message == "Invalid token.");
    }

    [Fact]
    public async Task ResetPassword_ValidToken_Succeeds()
    {
        var user = MakeUser();
        _userManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _userManager.Setup(m => m.ResetPasswordAsync(user, "good-token", "newpw")).ReturnsAsync(IdentityResult.Success);

        var result = await MakeService().ResetPassword(new ResetPasswordModel { UserId = user.Id, Token = "good-token", NewPassword = "newpw" });

        result.IsSuccess.Should().BeTrue();
    }
}
