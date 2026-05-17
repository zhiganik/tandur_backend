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
[Route("api/auth")]
public class AuthController(
    UserManager<AppUser> userManager,
    JwtService jwtService,
    IRefreshTokenService refreshTokenService,
    IOtpService otpService,
    IOtpSender otpSender,
    IOtpSessionService otpSessionService,
    IConfiguration configuration) : ControllerBase
{
    private static readonly TimeSpan PhoneOtpExpiry = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan EmailOtpExpiry = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan SessionExpiry = TimeSpan.FromMinutes(15);

    private TimeSpan MobileRefreshExpiry =>
        TimeSpan.FromDays(int.Parse(configuration["Jwt:RefreshExpiryDays"] ?? "30"));

    [HttpPost("phone")]
    public async Task<IActionResult> SendPhoneOtp([FromBody] SendPhoneOtpRequest request)
    {
        var code = await otpService.GenerateAsync($"phone:{request.PhoneNumber}", PhoneOtpExpiry);
        await otpSender.SendSmsAsync(request.PhoneNumber, code);
        return Ok(new { message = "OTP sent." });
    }

    [HttpPost("phone/verify")]
    public async Task<IActionResult> VerifyPhoneOtp([FromBody] VerifyPhoneOtpRequest request)
    {
        var valid = await otpService.VerifyAsync($"phone:{request.PhoneNumber}", request.Code);
        if (!valid)
            return BadRequest(new { message = "Invalid or expired OTP." });

        var sessionToken = await otpSessionService.CreateAsync(request.PhoneNumber, SessionExpiry);
        return Ok(new { sessionToken });
    }

    [HttpPost("email")]
    public async Task<IActionResult> SendEmailOtp([FromBody] SendEmailOtpRequest request)
    {
        var phone = await otpSessionService.GetPhoneAsync(request.SessionToken);
        if (phone is null)
            return Unauthorized(new { message = "Session expired. Please verify your phone again." });

        var code = await otpService.GenerateAsync($"email:{request.Email}", EmailOtpExpiry);
        await otpSender.SendEmailAsync(request.Email, code);
        return Ok(new { message = "OTP sent." });
    }

    [HttpPost("email/verify")]
    public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyEmailOtpRequest request)
    {
        var phone = await otpSessionService.GetPhoneAsync(request.SessionToken);
        if (phone is null)
            return Unauthorized(new { message = "Session expired. Please verify your phone again." });

        var valid = await otpService.VerifyAsync($"email:{request.Email}", request.Code);
        if (!valid)
            return BadRequest(new { message = "Invalid or expired OTP." });

        var updatedToken = await otpSessionService.CreateEmailVerifiedAsync(phone, request.Email, SessionExpiry);
        return Ok(new { sessionToken = updatedToken });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var session = await otpSessionService.GetSessionAsync(request.SessionToken);
        if (session is null)
            return Unauthorized(new { message = "Session expired. Please start over." });

        if (session.Email is not null && await userManager.FindByEmailAsync(session.Email) is { } existingUser)
        {
            if (!string.IsNullOrEmpty(existingUser.PhoneNumber) && existingUser.PhoneNumber != session.Phone)
                return Conflict(new { message = "An account with this email already exists." });

            await otpSessionService.InvalidateAsync(request.SessionToken);
            return Ok(await IssueTokensAsync(existingUser));
        }

        var nameParts = request.FullName.Trim().Split(' ', 2);
        var user = new AppUser
        {
            UserName = session.Email,
            Email = session.Email,
            PhoneNumber = session.Phone,
            FirstName = nameParts[0],
            LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
            PhoneNumberConfirmed = true,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await userManager.AddToRoleAsync(user, TandurRoles.User);
        await otpSessionService.InvalidateAsync(request.SessionToken);

        return Ok(await IssueTokensAsync(user));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var userId = await refreshTokenService.GetUserIdAsync(request.RefreshToken);
        if (userId is null)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized(new { message = "User not found." });

        await refreshTokenService.RevokeAsync(request.RefreshToken);
        return Ok(await IssueTokensAsync(user));
    }

    [HttpDelete("account")]
    [Authorize(Roles = TandurRoles.User)]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Forbid();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        await refreshTokenService.RevokeAllForUserAsync(userId);
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    private async Task<TokenResponse> IssueTokensAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var expiry = jwtService.GetExpiry();
        var accessToken = jwtService.GenerateToken(user, roles);
        var refreshToken = await refreshTokenService.CreateAsync(user.Id, MobileRefreshExpiry);
        return new TokenResponse(accessToken, refreshToken, expiry);
    }
}
