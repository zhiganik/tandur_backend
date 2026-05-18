using System.Security.Claims;
using Api.Controllers;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.DTOs.Auth;
using Core.Interfaces;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Api.Tests.Auth;

[TestFixture]
public class AdminAuthControllerTests
{
    private Mock<UserManager<AppUser>> _userManager         = null!;
    private Mock<IRefreshTokenService> _refreshTokenService = null!;
    private AdminAuthController        _controller          = null!;

    private const string AdminId  = "admin-abc";
    private const string RefreshT = "refresh-token-xyz";

    [SetUp]
    public void SetUp()
    {
        var store = new Mock<IUserStore<AppUser>>();
#pragma warning disable CS8625
        _userManager = new Mock<UserManager<AppUser>>(
            store.Object, null, null, null, null, null, null, null, null);
#pragma warning restore CS8625

        _refreshTokenService = new Mock<IRefreshTokenService>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"]                = "test-issuer",
                ["Jwt:Audience"]              = "test-audience",
                ["Jwt:ExpiryMinutes"]         = "15",
                ["Jwt:Key"]                   = "test-signing-key-must-be-at-least-32-bytes!!",
                ["Jwt:AdminRefreshExpiryDays"] = "2",
            })
            .Build();

        _controller = new AdminAuthController(
            _userManager.Object,
            new JwtService(configuration),
            _refreshTokenService.Object,
            new Mock<IAuthorizationService>().Object);

        SetUser(AdminId);
    }

    private void SetUser(string userId)
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId)], "Test"))
            }
        };
    }

    // Logout
    [Test]
    public async Task Logout_RevokesRefreshToken_Returns204()
    {
        _refreshTokenService.Setup(r => r.RevokeAsync(RefreshT)).Returns(Task.CompletedTask);

        var result = await _controller.Logout(new RefreshRequest { RefreshToken = RefreshT });

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        _refreshTokenService.Verify(r => r.RevokeAsync(RefreshT), Times.Once);
    }
}
