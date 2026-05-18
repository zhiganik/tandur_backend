using Core.Domain.Constants;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.DTOs.Auth;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Auth")]
[Produces("application/json")]
public class AuthController(
    UserManager<AppUser> userManager,
    JwtService jwtService,
    IRefreshTokenService refreshTokenService,
    IOtpSessionService otpSessionService,
    IUserRepository repository) : ControllerBase
{
    [HttpPost("login")]
    [SwaggerOperation(Summary = "Log in an existing user using an email-verified OTP session")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var session = await otpSessionService.GetSessionAsync(request.SessionToken);
        if (session?.Email is null)
            return Unauthorized(new { message = "Session expired. Please start over." });

        var user = await userManager.FindByEmailAsync(session.Email);
        if (user is null || user.PhoneNumber != session.Phone)
            return NotFound(new { message = "No account found. Please register." });

        await otpSessionService.InvalidateAsync(request.SessionToken);
        return Ok(await IssueTokensAsync(user, ClientType.Mobile));
    }

    [HttpPost("register")]
    [SwaggerOperation(Summary = "Register a new client user using an email-verified OTP session")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var session = await otpSessionService.GetSessionAsync(request.SessionToken);
        if (session?.Email is null)
            return Unauthorized(new { message = "Session expired. Please start over." });

        if (await repository.GetByEmailAsync(session.Email) is not null)
            return Conflict(new { message = "This email is already registered. Please log in." });

        if (await repository.GetByPhoneAsync(session.Phone) is not null)
            return Conflict(new { message = "This phone number is already registered. Please log in." });

        var (firstName, lastName) = SplitFullName(request.FullName);
        var user = new AppUser
        {
            UserName             = session.Email,
            Email                = session.Email,
            PhoneNumber          = session.Phone,
            FirstName            = firstName,
            LastName             = lastName,
            PhoneNumberConfirmed = true,
            EmailConfirmed       = true,
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await userManager.AddToRoleAsync(user, TandurRoles.User);
        await otpSessionService.InvalidateAsync(request.SessionToken);
        return Ok(await IssueTokensAsync(user, ClientType.Mobile));
    }

    [HttpPost("refresh")]
    [SwaggerOperation(Summary = "Rotate refresh token — works for both mobile and web clients")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var (userId, clientType) = await refreshTokenService.GetAsync(request.RefreshToken);
        if (userId is null || clientType is null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return Unauthorized(new { message = "User not found." });

        if (clientType == ClientType.Web)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Contains(TandurRoles.Admin) && !roles.Contains(TandurRoles.SuperAdmin))
                return Forbid();
        }

        await refreshTokenService.RevokeAsync(request.RefreshToken);
        return Ok(await IssueTokensAsync(user, clientType.Value));
    }

    private static (string firstName, string lastName) SplitFullName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', 2);
        return (parts[0], parts.Length > 1 ? parts[1] : string.Empty);
    }

    private async Task<TokenResponse> IssueTokensAsync(AppUser user, ClientType clientType)
    {
        var roles        = await userManager.GetRolesAsync(user);
        var expiry       = jwtService.GetExpiry();
        var accessToken  = jwtService.GenerateToken(user, roles);
        var refreshToken = await refreshTokenService.CreateAsync(user.Id, clientType);
        return new TokenResponse(accessToken, refreshToken, expiry);
    }
}
