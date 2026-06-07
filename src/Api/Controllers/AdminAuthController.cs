using System.Security.Claims;
using Api.Filters;
using Core.Domain.Constants;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.DTOs.Auth;
using Core.Interfaces;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
[Tags("Admin › Auth")]
[Produces("application/json")]
public class AdminAuthController(
    UserManager<AppUser> userManager,
    JwtService jwtService,
    IRefreshTokenService refreshTokenService,
    IAuthorizationService authorizationService,
    ILogger<AdminAuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    [SwaggerOperation(Summary = "Admin login with email and password")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            logger.LogWarning("Admin login failed for {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var roles = await userManager.GetRolesAsync(user);
        if (!await HasAdminAccessAsync(roles))
        {
            logger.LogWarning("Admin login blocked — {UserId} does not have admin role", user.Id);
            return Forbid();
        }

        if (user.MustChangePassword)
        {
            logger.LogInformation("Admin {UserId} requires password change on login", user.Id);
            var scopedToken = jwtService.GenerateToken(user, roles,
                extraClaims: [new Claim("scope", "change_password")]);
            return Ok(new { requiresPasswordChange = true, token = scopedToken });
        }

        logger.LogInformation("Admin {UserId} authenticated successfully", user.Id);
        return Ok(await IssueTokensAsync(user, roles));
    }

    [HttpPost("change-password")]
    [Authorize]
    [RequireScope("change_password")]
    [SwaggerOperation(Summary = "Change password (required on first login)")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);

        logger.LogInformation("Admin {UserId} changed password", userId);
        var roles = await userManager.GetRolesAsync(user);
        return Ok(await IssueTokensAsync(user, roles));
    }

    [HttpPost("logout")]
    [Authorize(Policy = TandurPolicies.AdminPanel)]
    [BlockScopedToken]
    [SwaggerOperation(Summary = "Logout — revoke current web refresh token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        await refreshTokenService.RevokeAsync(request.RefreshToken);
        return NoContent();
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
        var expiry       = jwtService.GetExpiry();
        var accessToken  = jwtService.GenerateToken(user, roles);
        var refreshToken = await refreshTokenService.CreateAsync(user.Id, ClientType.Web);
        return new TokenResponse(accessToken, refreshToken, expiry);
    }
}
