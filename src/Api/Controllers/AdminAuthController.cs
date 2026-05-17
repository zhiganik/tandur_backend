using System.Security.Claims;
using Core.Domain.Constants;
using Core.Domain.Entities;
using Core.DTOs.Auth;
using Core.Interfaces.Services;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController(
    UserManager<AppUser> userManager,
    JwtService jwtService,
    IRefreshTokenService refreshTokenService,
    IAuthorizationService authorizationService,
    IConfiguration configuration) : ControllerBase
{
    private TimeSpan AdminRefreshExpiry =>
        TimeSpan.FromDays(int.Parse(configuration["Jwt:AdminRefreshExpiryDays"] ?? "2"));

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        var roles = await userManager.GetRolesAsync(user);
        if (!await HasAdminAccessAsync(roles))
            return Forbid();

        if (user.MustChangePassword)
        {
            var scopedToken = jwtService.GenerateToken(user, roles,
                extraClaims: [new Claim("scope", "change_password")]);

            return Ok(new { requiresPasswordChange = true, token = scopedToken });
        }

        return Ok(await IssueTokensAsync(user, roles));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var scope = User.FindFirstValue("scope");
        if (scope != "change_password")
            return Forbid();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Forbid();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        return Ok(await IssueTokensAsync(user, roles));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var userId = await refreshTokenService.GetUserIdAsync(request.RefreshToken);
        if (userId is null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized(new { message = "User not found." });

        var roles = await userManager.GetRolesAsync(user);
        if (!await HasAdminAccessAsync(roles))
            return Forbid();

        await refreshTokenService.RevokeAsync(request.RefreshToken);
        return Ok(await IssueTokensAsync(user, roles));
    }

    private async Task<bool> HasAdminAccessAsync(IList<string> roles)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            roles.Select(r => new Claim(ClaimTypes.Role, r)), authenticationType: "Password"));
        var result = await authorizationService.AuthorizeAsync(principal, TandurPolicies.AdminPanel);
        return result.Succeeded;
    }

    private async Task<TokenResponse> IssueTokensAsync(AppUser user, IList<string> roles)
    {
        var expiry = jwtService.GetExpiry();
        var accessToken = jwtService.GenerateToken(user, roles);
        var refreshToken = await refreshTokenService.CreateAsync(user.Id, AdminRefreshExpiry, ClientTypes.Web);
        return new TokenResponse(accessToken, refreshToken, expiry);
    }
}
