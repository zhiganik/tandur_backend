using System.Security.Claims;
using Api.Controllers;
using Core.Domain.Constants;
using Core.Domain.Entities;
using Core.DTOs.Auth;
using Core.DTOs.Me;
using Core.DTOs.Users;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Me;

[TestFixture]
public class MeControllerTests
{
    private Mock<IUserService>          _userService          = null!;
    private Mock<IOtpService>           _otpService           = null!;
    private Mock<IOtpSender>            _otpSender            = null!;
    private Mock<IRefreshTokenService>  _refreshTokenService  = null!;
    private Mock<UserManager<AppUser>>  _userManager          = null!;
    private Mock<IUserRepository>       _repository           = null!;
    private Mock<IOtpRateLimiter>       _rateLimiter          = null!;
    private Mock<IPasswordResetSender>  _passwordResetSender  = null!;
    private MeController                _controller           = null!;

    private const string UserId   = "user-123";
    private const string NewPhone = "+79001234567";
    private const string NewEmail = "new@example.com";
    private const string OtpCode  = "111111";

    [SetUp]
    public void SetUp()
    {
        _userService          = new Mock<IUserService>();
        _otpService           = new Mock<IOtpService>();
        _otpSender            = new Mock<IOtpSender>();
        _refreshTokenService  = new Mock<IRefreshTokenService>();
        _repository           = new Mock<IUserRepository>();
        _rateLimiter          = new Mock<IOtpRateLimiter>();
        _passwordResetSender  = new Mock<IPasswordResetSender>();
        _rateLimiter.Setup(r => r.TryRecordAsync(It.IsAny<string>()))
            .ReturnsAsync(new RateLimitResult(true, 60));

        var store = new Mock<IUserStore<AppUser>>();
#pragma warning disable CS8625
        _userManager = new Mock<UserManager<AppUser>>(
            store.Object, null, null, null, null, null, null, null, null);
#pragma warning restore CS8625

        _controller = new MeController(
            _userService.Object,
            _otpService.Object,
            _otpSender.Object,
            _refreshTokenService.Object,
            _userManager.Object,
            _repository.Object,
            _rateLimiter.Object,
            _passwordResetSender.Object);

        SetUser(UserId);
    }

    private void SetUser(string userId, string? scope = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        if (scope is not null) claims.Add(new Claim("scope", scope));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
    }

    private static MeDto MakeDto() => new(
        Id: UserId, FirstName: "John", LastName: "Doe",
        Email: "john@example.com", EmailConfirmed: true,
        Phone: "+79000000000", PhoneNumberConfirmed: true,
        DateOfBirth: null, Roles: ["User"], CreatedAt: DateTime.UtcNow,
        Restaurants: []);

    // GetMe
    [Test]
    public async Task GetMe_ReturnsOkWithDto()
    {
        _userService.Setup(s => s.GetMeAsync(UserId)).ReturnsAsync(MakeDto());

        Assert.That(await _controller.GetMe(), Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public async Task GetMe_UserNotFound_Returns404()
    {
        _userService.Setup(s => s.GetMeAsync(UserId)).ReturnsAsync((MeDto?)null);

        Assert.That(await _controller.GetMe(), Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetMe_ScopedToken_ReturnsForbid()
    {
        SetUser(UserId, scope: "change_password");

        Assert.That(await _controller.GetMe(), Is.InstanceOf<ForbidResult>());
    }

    // UpdateProfile (PATCH /me)
    [Test]
    public async Task UpdateProfile_NameAndBirthday_Returns204()
    {
        var birthday = new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        _userService.Setup(s => s.UpdateNameAsync(UserId, "John", "Doe"))
            .ReturnsAsync(new UserUpdateResult.Success());
        _userService.Setup(s => s.SetBirthdayAsync(UserId, birthday))
            .ReturnsAsync(new UserUpdateResult.Success());

        Assert.That(
            await _controller.UpdateProfile(new UpdateMeRequest { FirstName = "John", LastName = "Doe", DateOfBirth = birthday }),
            Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task UpdateProfile_NoBirthday_ClearsBirthday()
    {
        _userService.Setup(s => s.UpdateNameAsync(UserId, "John", "Doe"))
            .ReturnsAsync(new UserUpdateResult.Success());
        _userService.Setup(s => s.SetBirthdayAsync(UserId, null))
            .ReturnsAsync(new UserUpdateResult.Success());

        await _controller.UpdateProfile(new UpdateMeRequest { FirstName = "John", LastName = "Doe" });

        _userService.Verify(s => s.SetBirthdayAsync(UserId, null), Times.Once);
    }

    [Test]
    public async Task UpdateProfile_NameNotFound_Returns404()
    {
        _userService.Setup(s => s.UpdateNameAsync(UserId, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new UserUpdateResult.NotFound());

        Assert.That(
            await _controller.UpdateProfile(new UpdateMeRequest { FirstName = "A", LastName = "B" }),
            Is.InstanceOf<NotFoundResult>());
    }

    // SendPhoneChangeOtp
    [Test]
    public async Task SendPhoneChangeOtp_UsesCorrectKeyAndSendsSms()
    {
        _repository.Setup(r => r.GetByPhoneAsync(NewPhone, UserId)).ReturnsAsync((AppUser?)null);
        _otpService.Setup(s => s.GenerateAsync(OtpKeys.PhoneChange(UserId, NewPhone), It.IsAny<TimeSpan>()))
            .ReturnsAsync(OtpCode);

        await _controller.SendPhoneChangeOtp(new ChangePhoneRequest { NewPhone = NewPhone });

        _otpSender.Verify(s => s.SendSmsAsync(NewPhone, OtpCode), Times.Once);
    }

    [Test]
    public async Task SendPhoneChangeOtp_DuplicatePhone_Returns409()
    {
        _repository.Setup(r => r.GetByPhoneAsync(NewPhone, UserId))
            .ReturnsAsync(new AppUser { Id = "other", PhoneNumber = NewPhone });

        var result = await _controller.SendPhoneChangeOtp(new ChangePhoneRequest { NewPhone = NewPhone });

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        _otpSender.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task SendPhoneChangeOtp_RateLimited_Returns429()
    {
        _repository.Setup(r => r.GetByPhoneAsync(NewPhone, UserId)).ReturnsAsync((AppUser?)null);
        _rateLimiter.Setup(r => r.TryRecordAsync(OtpKeys.PhoneChange(UserId, NewPhone)))
            .ReturnsAsync(new RateLimitResult(false, 55));

        var result = await _controller.SendPhoneChangeOtp(new ChangePhoneRequest { NewPhone = NewPhone });

        Assert.That((result as ObjectResult)!.StatusCode, Is.EqualTo(429));
        _otpSender.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task SendPhoneChangeOtp_ScopedToken_ReturnsForbid()
    {
        SetUser(UserId, scope: "change_password");

        Assert.That(
            await _controller.SendPhoneChangeOtp(new ChangePhoneRequest { NewPhone = NewPhone }),
            Is.InstanceOf<ForbidResult>());
    }

    // UpdatePhone
    [Test]
    public async Task UpdatePhone_InvalidOtp_Returns400()
    {
        _repository.Setup(r => r.GetByPhoneAsync(NewPhone, UserId)).ReturnsAsync((AppUser?)null);
        _otpService.Setup(s => s.VerifyAsync(OtpKeys.PhoneChange(UserId, NewPhone), OtpCode)).ReturnsAsync(false);

        var result = await _controller.UpdatePhone(new VerifyPhoneChangeRequest { NewPhone = NewPhone, Code = OtpCode });

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _userService.Verify(s => s.SetVerifiedPhoneAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task UpdatePhone_ValidOtp_Returns204()
    {
        _repository.Setup(r => r.GetByPhoneAsync(NewPhone, UserId)).ReturnsAsync((AppUser?)null);
        _otpService.Setup(s => s.VerifyAsync(OtpKeys.PhoneChange(UserId, NewPhone), OtpCode)).ReturnsAsync(true);
        _userService.Setup(s => s.SetVerifiedPhoneAsync(UserId, NewPhone)).ReturnsAsync(new UserUpdateResult.Success());

        Assert.That(
            await _controller.UpdatePhone(new VerifyPhoneChangeRequest { NewPhone = NewPhone, Code = OtpCode }),
            Is.InstanceOf<NoContentResult>());
    }

    // SendEmailChangeOtp
    [Test]
    public async Task SendEmailChangeOtp_UsesCorrectKeyAndSendsEmail()
    {
        _repository.Setup(r => r.GetByEmailAsync(NewEmail)).ReturnsAsync((AppUser?)null);
        _otpService.Setup(s => s.GenerateAsync(OtpKeys.EmailChange(UserId, NewEmail), It.IsAny<TimeSpan>()))
            .ReturnsAsync(OtpCode);

        await _controller.SendEmailChangeOtp(new ChangeEmailRequest { NewEmail = NewEmail });

        _otpSender.Verify(s => s.SendEmailAsync(NewEmail, OtpCode), Times.Once);
    }

    [Test]
    public async Task SendEmailChangeOtp_DuplicateEmail_Returns409()
    {
        _repository.Setup(r => r.GetByEmailAsync(NewEmail))
            .ReturnsAsync(new AppUser { Id = "other", Email = NewEmail });

        var result = await _controller.SendEmailChangeOtp(new ChangeEmailRequest { NewEmail = NewEmail });

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        _otpSender.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // UpdateEmail
    [Test]
    public async Task UpdateEmail_InvalidOtp_Returns400()
    {
        _repository.Setup(r => r.GetByEmailAsync(NewEmail)).ReturnsAsync((AppUser?)null);
        _otpService.Setup(s => s.VerifyAsync(OtpKeys.EmailChange(UserId, NewEmail), OtpCode)).ReturnsAsync(false);

        Assert.That(
            await _controller.UpdateEmail(new VerifyEmailChangeRequest { NewEmail = NewEmail, Code = OtpCode }),
            Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateEmail_ValidOtp_Returns204()
    {
        _repository.Setup(r => r.GetByEmailAsync(NewEmail)).ReturnsAsync((AppUser?)null);
        _otpService.Setup(s => s.VerifyAsync(OtpKeys.EmailChange(UserId, NewEmail), OtpCode)).ReturnsAsync(true);
        _userService.Setup(s => s.SetVerifiedEmailAsync(UserId, NewEmail)).ReturnsAsync(new UserUpdateResult.Success());

        Assert.That(
            await _controller.UpdateEmail(new VerifyEmailChangeRequest { NewEmail = NewEmail, Code = OtpCode }),
            Is.InstanceOf<NoContentResult>());
    }

    // SendPasswordResetToken
    [Test]
    public async Task SendPasswordResetToken_SendsTokenAndReturns200()
    {
        var user = new AppUser { Id = UserId, Email = "admin@example.com" };
        _userManager.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync(user);
        _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token-abc");
        _passwordResetSender.Setup(s => s.SendAsync(user.Email, "reset-token-abc")).Returns(Task.CompletedTask);

        var result = await _controller.SendPasswordResetToken();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _passwordResetSender.Verify(s => s.SendAsync(user.Email, "reset-token-abc"), Times.Once);
    }

    // ResetPassword
    [Test]
    public async Task ResetPassword_ValidToken_ResetsAndRevokesTokens()
    {
        var user = new AppUser { Id = UserId, Email = "admin@example.com" };
        _userManager.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync(user);
        _userManager.Setup(m => m.ResetPasswordAsync(user, "reset-token", "NewPass1!"))
            .ReturnsAsync(IdentityResult.Success);
        _refreshTokenService.Setup(r => r.RevokeAllForUserAsync(UserId)).Returns(Task.CompletedTask);

        var result = await _controller.ResetPassword(
            new ResetPasswordWithTokenRequest { Token = "reset-token", NewPassword = "NewPass1!" });

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        _refreshTokenService.Verify(r => r.RevokeAllForUserAsync(UserId), Times.Once);
    }

    [Test]
    public async Task ResetPassword_InvalidToken_Returns400()
    {
        var user = new AppUser { Id = UserId };
        _userManager.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync(user);
        _userManager.Setup(m => m.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var result = await _controller.ResetPassword(
            new ResetPasswordWithTokenRequest { Token = "bad-token", NewPassword = "NewPass1!" });

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        _refreshTokenService.Verify(r => r.RevokeAllForUserAsync(It.IsAny<string>()), Times.Never);
    }

    // SendDeleteOtp
    [Test]
    public async Task SendDeleteOtp_HasPhone_SendsOtp()
    {
        var user = new AppUser { Id = UserId, PhoneNumber = "+79000000000" };
        _userManager.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync(user);
        _otpService.Setup(s => s.GenerateAsync(OtpKeys.AccountDelete(UserId), It.IsAny<TimeSpan>()))
            .ReturnsAsync(OtpCode);

        var result = await _controller.SendDeleteOtp();

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _otpSender.Verify(s => s.SendSmsAsync(user.PhoneNumber, OtpCode), Times.Once);
    }

    [Test]
    public async Task SendDeleteOtp_NoPhone_Returns400()
    {
        _userManager.Setup(m => m.FindByIdAsync(UserId))
            .ReturnsAsync(new AppUser { Id = UserId, PhoneNumber = null });

        Assert.That(await _controller.SendDeleteOtp(), Is.InstanceOf<BadRequestObjectResult>());
        _otpSender.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // DeleteAccount
    [Test]
    public async Task DeleteAccount_InvalidOtp_Returns400()
    {
        _otpService.Setup(s => s.VerifyAsync(OtpKeys.AccountDelete(UserId), OtpCode)).ReturnsAsync(false);

        Assert.That(
            await _controller.DeleteAccount(new DeleteAccountRequest { Code = OtpCode }),
            Is.InstanceOf<BadRequestObjectResult>());
        _userManager.Verify(m => m.DeleteAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Test]
    public async Task DeleteAccount_ValidOtp_RevokesAndDeletes()
    {
        var user = new AppUser { Id = UserId };
        _otpService.Setup(s => s.VerifyAsync(OtpKeys.AccountDelete(UserId), OtpCode)).ReturnsAsync(true);
        _userManager.Setup(m => m.FindByIdAsync(UserId)).ReturnsAsync(user);
        _userManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);
        _refreshTokenService.Setup(r => r.RevokeAllForUserAsync(UserId)).Returns(Task.CompletedTask);

        Assert.That(
            await _controller.DeleteAccount(new DeleteAccountRequest { Code = OtpCode }),
            Is.InstanceOf<NoContentResult>());
        _refreshTokenService.Verify(r => r.RevokeAllForUserAsync(UserId), Times.Once);
    }
}
