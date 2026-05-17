using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Controllers;
using Core.Domain.Constants;
using Core.Domain.Entities;
using Core.DTOs.Auth;
using Core.Interfaces;
using Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Api.Tests.Auth;

[TestFixture]
public class RegisterTests
{
    private Mock<UserManager<AppUser>> _userManager = null!;
    private Mock<IRefreshTokenService> _refreshTokenService = null!;
    private Mock<IOtpService> _otpService = null!;
    private Mock<IOtpSender> _otpSender = null!;
    private Mock<IOtpSessionService> _otpSessionService = null!;
    private AuthController _controller = null!;

    private const string SessionToken = "test-session-token";
    private const string Email = "superadmin@tandur.com";
    private const string Phone = "+79991234567";

    [SetUp]
    public void SetUp()
    {
        var store = new Mock<IUserStore<AppUser>>();
#pragma warning disable CS8625
        _userManager = new Mock<UserManager<AppUser>>(
            store.Object, null, null, null, null, null, null, null, null);
#pragma warning restore CS8625

        _refreshTokenService = new Mock<IRefreshTokenService>();
        _otpService = new Mock<IOtpService>();
        _otpSender = new Mock<IOtpSender>();
        _otpSessionService = new Mock<IOtpSessionService>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpiryMinutes"] = "15",
                ["Jwt:Key"] = "test-signing-key-must-be-at-least-32-bytes!!",
                ["Jwt:RefreshExpiryDays"] = "30",
            })
            .Build();

        var jwtService = new JwtService(configuration);

        _controller = new AuthController(
            _userManager.Object,
            jwtService,
            _refreshTokenService.Object,
            _otpService.Object,
            _otpSender.Object,
            _otpSessionService.Object,
            configuration);
    }

    [Test]
    public async Task Register_SuperAdminWithPhoneAlreadySet_LogsInAndAddsUserRole()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = Email,
            UserName = Email,
            PhoneNumber = Phone,
            PhoneNumberConfirmed = true,
        };

        SetupExistingUserFlow(user, existingRoles: [TandurRoles.SuperAdmin, TandurRoles.User], hasUserRole: false);

        var result = await _controller.Register(new RegisterRequest { SessionToken = SessionToken });

        AssertTokenContainsRoles(result, TandurRoles.SuperAdmin, TandurRoles.User);
        _userManager.Verify(x => x.AddToRoleAsync(user, TandurRoles.User), Times.Once);
        _otpSessionService.Verify(x => x.InvalidateAsync(SessionToken), Times.Once);
    }

    [Test]
    public async Task Register_SuperAdminWithPhoneAlreadySet_AlreadyHasUserRole_DoesNotAddRoleAgain()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = Email,
            UserName = Email,
            PhoneNumber = Phone,
            PhoneNumberConfirmed = true,
        };

        SetupExistingUserFlow(user, existingRoles: [TandurRoles.SuperAdmin, TandurRoles.User], hasUserRole: true);

        var result = await _controller.Register(new RegisterRequest { SessionToken = SessionToken });

        AssertTokenContainsRoles(result, TandurRoles.SuperAdmin, TandurRoles.User);
        _userManager.Verify(x => x.AddToRoleAsync(user, TandurRoles.User), Times.Never);
    }

    [Test]
    public async Task Register_SuperAdminWithoutPhone_ReturnsConflict()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = Email,
            UserName = Email,
            PhoneNumber = null,
        };

        _otpSessionService.Setup(x => x.GetSessionAsync(SessionToken))
            .ReturnsAsync(new OtpSession(Phone, Email));

        _userManager.Setup(x => x.FindByEmailAsync(Email))
            .ReturnsAsync(user);

        var result = await _controller.Register(new RegisterRequest { SessionToken = SessionToken });

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        _userManager.Verify(x => x.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Register_SuperAdminWithDifferentPhone_ReturnsConflict()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = Email,
            UserName = Email,
            PhoneNumber = "+70000000000",
        };

        _otpSessionService.Setup(x => x.GetSessionAsync(SessionToken))
            .ReturnsAsync(new OtpSession(Phone, Email));

        _userManager.Setup(x => x.FindByEmailAsync(Email))
            .ReturnsAsync(user);

        var result = await _controller.Register(new RegisterRequest { SessionToken = SessionToken });

        Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        _userManager.Verify(x => x.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }

    private void SetupExistingUserFlow(AppUser user, List<string> existingRoles, bool hasUserRole)
    {
        _otpSessionService.Setup(x => x.GetSessionAsync(SessionToken))
            .ReturnsAsync(new OtpSession(Phone, Email));

        _userManager.Setup(x => x.FindByEmailAsync(Email))
            .ReturnsAsync(user);

        _userManager.Setup(x => x.IsInRoleAsync(user, TandurRoles.User))
            .ReturnsAsync(hasUserRole);

        _userManager.Setup(x => x.AddToRoleAsync(user, TandurRoles.User))
            .ReturnsAsync(IdentityResult.Success);

        _userManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(existingRoles);

        _refreshTokenService
            .Setup(x => x.CreateAsync(user.Id, It.IsAny<TimeSpan>(), ClientTypes.Mobile))
            .ReturnsAsync("mock-refresh-token");

        _otpSessionService.Setup(x => x.InvalidateAsync(SessionToken))
            .Returns(Task.CompletedTask);
    }

    private static void AssertTokenContainsRoles(IActionResult result, params string[] expectedRoles)
    {
        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null, "Expected 200 OK");

        var response = ok!.Value as TokenResponse;
        Assert.That(response, Is.Not.Null, "Expected TokenResponse body");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response!.AccessToken);
        var roles = jwt.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        foreach (var role in expectedRoles)
            Assert.That(roles, Contains.Item(role), $"Expected role '{role}' in token");
    }
}
