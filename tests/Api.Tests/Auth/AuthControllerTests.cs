using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Controllers;
using Core.Domain.Constants;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.DTOs.Auth;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Api.Tests.Auth;

[TestFixture]
public class AuthControllerTests
{
    private Mock<UserManager<AppUser>> _userManager         = null!;
    private Mock<IRefreshTokenService> _refreshTokenService = null!;
    private Mock<IOtpSessionService>   _otpSessionService   = null!;
    private Mock<IUserRepository>      _repository          = null!;
    private AuthController             _controller          = null!;

    private const string SessionToken = "test-session-token";
    private const string RefreshToken = "test-refresh-token";
    private const string Email        = "user@tandur.com";
    private const string Phone        = "+79991234567";

    [SetUp]
    public void SetUp()
    {
        var store = new Mock<IUserStore<AppUser>>();
#pragma warning disable CS8625
        _userManager = new Mock<UserManager<AppUser>>(
            store.Object, null, null, null, null, null, null, null, null);
#pragma warning restore CS8625

        _refreshTokenService = new Mock<IRefreshTokenService>();
        _otpSessionService   = new Mock<IOtpSessionService>();
        _repository          = new Mock<IUserRepository>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"]            = "test-issuer",
                ["Jwt:Audience"]          = "test-audience",
                ["Jwt:ExpiryMinutes"]     = "15",
                ["Jwt:Key"]               = "test-signing-key-must-be-at-least-32-bytes!!",
                ["Jwt:RefreshExpiryDays"] = "30",
            })
            .Build();

        _controller = new AuthController(
            _userManager.Object,
            new JwtService(configuration),
            _refreshTokenService.Object,
            _otpSessionService.Object,
            _repository.Object);
    }

    // Login

    [Test]
    public async Task Login_ExistingUserPhoneMatch_ReturnsToken()
    {
        var user = MakeUser(Phone);
        _otpSessionService.Setup(x => x.GetSessionAsync(SessionToken))
            .ReturnsAsync(new OtpSession(Phone, Email));
        _userManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync(user);
        _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([TandurRoles.User]);
        _refreshTokenService.Setup(x => x.CreateAsync(user.Id, ClientType.Mobile))
            .ReturnsAsync("mock-refresh");
        _otpSessionService.Setup(x => x.InvalidateAsync(SessionToken)).Returns(Task.CompletedTask);

        var result = await _controller.Login(new LoginRequest { SessionToken = SessionToken });

        AssertTokenContainsRole(result, TandurRoles.User);
        _otpSessionService.Verify(x => x.InvalidateAsync(SessionToken), Times.Once);
    }

    [Test]
    public async Task Login_NoAccount_Returns404()
    {
        _otpSessionService.Setup(x => x.GetSessionAsync(SessionToken))
            .ReturnsAsync(new OtpSession(Phone, Email));
        _userManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync((AppUser?)null);

        var result = await _controller.Login(new LoginRequest { SessionToken = SessionToken });

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Login_PhoneMismatch_Returns404()
    {
        var user = MakeUser("+70000000000");
        _otpSessionService.Setup(x => x.GetSessionAsync(SessionToken))
            .ReturnsAsync(new OtpSession(Phone, Email));
        _userManager.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync(user);

        var result = await _controller.Login(new LoginRequest { SessionToken = SessionToken });

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Login_ExpiredSession_ReturnsUnauthorized()
    {
        _otpSessionService.Setup(x => x.GetSessionAsync(SessionToken))
            .ReturnsAsync((OtpSession?)null);

        var result = await _controller.Login(new LoginRequest { SessionToken = SessionToken });

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Login_SessionWithoutEmail_ReturnsUnauthorized()
    {
        _otpSessionService.Setup(x => x.GetSessionAsync(SessionToken))
            .ReturnsAsync(new OtpSession(Phone, null));

        var result = await _controller.Login(new LoginRequest { SessionToken = SessionToken });

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    // Register

    [Test]
    public async Task Register_NewUser_CreatesUserAndReturnsToken()
    {
        _otpSessionService.Setup(x => x.GetSessionAsync(SessionToken))
            .ReturnsAsync(new OtpSession(Phone, Email));
        _repository.Setup(r => r.GetByEmailAsync(Email)).ReturnsAsync((AppUser?)null);
        _repository.Setup(r => r.GetByPhoneAsync(Phone, null)).ReturnsAsync((AppUser?)null);
        _userManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(x => x.AddToRoleAsync(It.IsAny<AppUser>(), TandurRoles.User))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>()))
            .ReturnsAsync([TandurRoles.User]);
        _refreshTokenService.Setup(x => x.CreateAsync(It.IsAny<string>(), ClientType.Mobile))
            .ReturnsAsync("mock-refresh");
        _otpSessionService.Setup(x => x.InvalidateAsync(SessionToken)).Returns(Task.CompletedTask);

        var result = await _controller.Register(
            new RegisterRequest { SessionToken = SessionToken, FullName = "John Doe" });

        AssertTokenContainsRole(result, TandurRoles.User);
        _userManager.Verify(x => x.CreateAsync(It.Is<AppUser>(u =>
            u.Email == Email && u.PhoneNumber == Phone &&
            u.FirstName == "John" && u.LastName == "Doe" &&
            u.PhoneNumberConfirmed && u.EmailConfirmed)), Times.Once);
    }

    [Test]
    public async Task Register_DuplicateEmail_Returns409()
    {
        _otpSessionService.Setup(x => x.GetSessionAsync(SessionToken))
            .ReturnsAsync(new OtpSession(Phone, Email));
        _repository.Setup(r => r.GetByEmailAsync(Email))
            .ReturnsAsync(new AppUser { Id = "other-id", Email = Email });

        var result = await _controller.Register(
            new RegisterRequest { SessionToken = SessionToken, FullName = "John Doe" });

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        _userManager.Verify(x => x.CreateAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Test]
    public async Task Register_DuplicatePhone_Returns409()
    {
        _otpSessionService.Setup(x => x.GetSessionAsync(SessionToken))
            .ReturnsAsync(new OtpSession(Phone, Email));
        _repository.Setup(r => r.GetByEmailAsync(Email)).ReturnsAsync((AppUser?)null);
        _repository.Setup(r => r.GetByPhoneAsync(Phone, null))
            .ReturnsAsync(new AppUser { Id = "other-id", PhoneNumber = Phone });

        var result = await _controller.Register(
            new RegisterRequest { SessionToken = SessionToken, FullName = "John Doe" });

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        _userManager.Verify(x => x.CreateAsync(It.IsAny<AppUser>()), Times.Never);
    }

    [Test]
    public async Task Register_SessionWithoutEmail_ReturnsUnauthorized()
    {
        _otpSessionService.Setup(x => x.GetSessionAsync(SessionToken))
            .ReturnsAsync(new OtpSession(Phone, null));

        var result = await _controller.Register(
            new RegisterRequest { SessionToken = SessionToken, FullName = "John Doe" });

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        _userManager.Verify(x => x.CreateAsync(It.IsAny<AppUser>()), Times.Never);
    }

    // Refresh

    [Test]
    public async Task Refresh_ValidMobileToken_ReturnsNewToken()
    {
        var user = MakeUser(Phone);
        _refreshTokenService.Setup(x => x.GetAsync(RefreshToken))
            .ReturnsAsync((user.Id, (ClientType?)ClientType.Mobile));
        _userManager.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([TandurRoles.User]);
        _refreshTokenService.Setup(x => x.RevokeAsync(RefreshToken)).Returns(Task.CompletedTask);
        _refreshTokenService.Setup(x => x.CreateAsync(user.Id, ClientType.Mobile))
            .ReturnsAsync("new-refresh");

        var result = await _controller.Refresh(new RefreshRequest { RefreshToken = RefreshToken });

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        _refreshTokenService.Verify(x => x.RevokeAsync(RefreshToken), Times.Once);
    }

    [Test]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        _refreshTokenService.Setup(x => x.GetAsync(RefreshToken))
            .ReturnsAsync(((string?)null, (ClientType?)null));

        var result = await _controller.Refresh(new RefreshRequest { RefreshToken = RefreshToken });

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    private static AppUser MakeUser(string? phone) => new()
    {
        Id          = Guid.NewGuid().ToString(),
        Email       = Email,
        UserName    = Email,
        PhoneNumber = phone,
    };

    private static void AssertTokenContainsRole(IActionResult result, string expectedRole)
    {
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null, "Expected 200 OK");
        var response = ok!.Value as TokenResponse;
        Assert.That(response, Is.Not.Null, "Expected TokenResponse body");
        var jwt   = new JwtSecurityTokenHandler().ReadJwtToken(response!.AccessToken);
        var roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.That(roles, Contains.Item(expectedRole));
    }
}
