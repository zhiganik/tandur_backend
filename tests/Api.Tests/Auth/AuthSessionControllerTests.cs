using Api.Controllers;
using Core.Domain.Constants;
using Core.DTOs.Auth;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Auth;

[TestFixture]
public class AuthSessionControllerTests
{
    private Mock<IOtpService>        _otpService        = null!;
    private Mock<IOtpSender>         _otpSender         = null!;
    private Mock<IOtpSessionService> _otpSessionService = null!;
    private Mock<IOtpRateLimiter>    _rateLimiter       = null!;
    private AuthSessionController    _controller        = null!;

    private const string SessionToken = "session-token";
    private const string Phone        = "+79991234567";
    private const string Email        = "user@tandur.com";
    private const string Code         = "111111";

    [SetUp]
    public void SetUp()
    {
        _otpService        = new Mock<IOtpService>();
        _otpSender         = new Mock<IOtpSender>();
        _otpSessionService = new Mock<IOtpSessionService>();
        _rateLimiter       = new Mock<IOtpRateLimiter>();
        _rateLimiter.Setup(r => r.TryRecordAsync(It.IsAny<string>()))
            .ReturnsAsync(new RateLimitResult(true, 60));
        _controller = new AuthSessionController(
            _otpService.Object, _otpSender.Object, _otpSessionService.Object, _rateLimiter.Object);
    }

    [Test]
    public async Task SendPhoneOtp_GeneratesAndSendsOtp()
    {
        _otpService.Setup(s => s.GenerateAsync(OtpKeys.Phone(Phone), It.IsAny<TimeSpan>())).ReturnsAsync(Code);

        var result = await _controller.SendPhoneOtp(new SendPhoneOtpRequest { PhoneNumber = Phone });

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _otpSender.Verify(s => s.SendSmsAsync(Phone, Code), Times.Once);
    }

    [Test]
    public async Task SendPhoneOtp_RateLimited_Returns429()
    {
        _rateLimiter.Setup(r => r.TryRecordAsync(OtpKeys.Phone(Phone)))
            .ReturnsAsync(new RateLimitResult(false, 42));

        var result = await _controller.SendPhoneOtp(new SendPhoneOtpRequest { PhoneNumber = Phone });

        Assert.That((result as ObjectResult)!.StatusCode, Is.EqualTo(429));
        _otpSender.Verify(s => s.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task VerifyPhoneOtp_ValidCode_ReturnsSessionToken()
    {
        _otpService.Setup(s => s.VerifyAsync(OtpKeys.Phone(Phone), Code)).ReturnsAsync(true);
        _otpSessionService.Setup(s => s.CreateAsync(Phone, It.IsAny<TimeSpan>())).ReturnsAsync("new-session");

        var result = await _controller.VerifyPhoneOtp(new VerifyPhoneOtpRequest { PhoneNumber = Phone, Code = Code });

        Assert.That((result as OkObjectResult)!.Value!.ToString(), Does.Contain("new-session"));
    }

    [Test]
    public async Task VerifyPhoneOtp_InvalidCode_Returns400()
    {
        _otpService.Setup(s => s.VerifyAsync(OtpKeys.Phone(Phone), Code)).ReturnsAsync(false);

        Assert.That(
            await _controller.VerifyPhoneOtp(new VerifyPhoneOtpRequest { PhoneNumber = Phone, Code = Code }),
            Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task SendEmailOtp_ValidSession_SendsOtp()
    {
        _otpSessionService.Setup(s => s.GetPhoneAsync(SessionToken)).ReturnsAsync(Phone);
        _otpService.Setup(s => s.GenerateAsync(OtpKeys.Email(Email), It.IsAny<TimeSpan>())).ReturnsAsync(Code);

        var result = await _controller.SendEmailOtp(new SendEmailOtpRequest { SessionToken = SessionToken, Email = Email });

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _otpSender.Verify(s => s.SendEmailAsync(Email, Code), Times.Once);
    }

    [Test]
    public async Task SendEmailOtp_ExpiredSession_ReturnsUnauthorized()
    {
        _otpSessionService.Setup(s => s.GetPhoneAsync(SessionToken)).ReturnsAsync((string?)null);

        Assert.That(
            await _controller.SendEmailOtp(new SendEmailOtpRequest { SessionToken = SessionToken, Email = Email }),
            Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task VerifyEmailOtp_ValidCode_ReturnsUpgradedSessionToken()
    {
        _otpSessionService.Setup(s => s.GetPhoneAsync(SessionToken)).ReturnsAsync(Phone);
        _otpService.Setup(s => s.VerifyAsync(OtpKeys.Email(Email), Code)).ReturnsAsync(true);
        _otpSessionService.Setup(s => s.CreateEmailVerifiedAsync(Phone, Email, It.IsAny<TimeSpan>()))
            .ReturnsAsync("upgraded-session");

        var result = await _controller.VerifyEmailOtp(
            new VerifyEmailOtpRequest { SessionToken = SessionToken, Email = Email, Code = Code });

        Assert.That((result as OkObjectResult)!.Value!.ToString(), Does.Contain("upgraded-session"));
    }

    [Test]
    public async Task VerifyEmailOtp_InvalidCode_Returns400()
    {
        _otpSessionService.Setup(s => s.GetPhoneAsync(SessionToken)).ReturnsAsync(Phone);
        _otpService.Setup(s => s.VerifyAsync(OtpKeys.Email(Email), Code)).ReturnsAsync(false);

        Assert.That(
            await _controller.VerifyEmailOtp(new VerifyEmailOtpRequest { SessionToken = SessionToken, Email = Email, Code = Code }),
            Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task VerifyEmailOtp_ExpiredSession_ReturnsUnauthorized()
    {
        _otpSessionService.Setup(s => s.GetPhoneAsync(SessionToken)).ReturnsAsync((string?)null);

        Assert.That(
            await _controller.VerifyEmailOtp(new VerifyEmailOtpRequest { SessionToken = SessionToken, Email = Email, Code = Code }),
            Is.InstanceOf<UnauthorizedObjectResult>());
    }
}
