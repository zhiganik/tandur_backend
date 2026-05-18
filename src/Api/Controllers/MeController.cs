using System.Security.Claims;
using Core.Domain.Constants;
using Core.Domain.Entities;
using Core.DTOs.Auth;
using Core.DTOs.Me;
using Core.DTOs.Users;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;


namespace Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
[Tags("Me")]
[Produces("application/json")]
public class MeController(
    IUserService userService,
    IOtpService otpService,
    IOtpSender otpSender,
    IRefreshTokenService refreshTokenService,
    UserManager<AppUser> userManager,
    IUserRepository repository,
    IOtpRateLimiter rateLimiter,
    IPasswordResetSender passwordResetSender) : ControllerBase
{
    private static readonly TimeSpan OtpExpiry = TimeSpan.FromMinutes(10);

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private IActionResult? BlockIfScopedToken()
    {
        var scope = User.FindFirstValue("scope");
        return scope == "change_password" ? Forbid() : null;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "Get own profile")]
    [ProducesResponseType<MeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe()
    {
        if (BlockIfScopedToken() is { } block) return block;
        if (UserId is null) return Forbid();

        var me = await userService.GetMeAsync(UserId);
        return me is null ? NotFound() : Ok(me);
    }

    [HttpPatch]
    [SwaggerOperation(Summary = "Update name and/or date of birth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateMeRequest request)
    {
        if (BlockIfScopedToken() is { } block) return block;
        if (UserId is null) return Forbid();

        var nameResult = await userService.UpdateNameAsync(UserId, request.FirstName, request.LastName);
        if (nameResult is not UserUpdateResult.Success) return MapResult(nameResult);

        var birthday = request.DateOfBirth.HasValue
            ? DateTime.SpecifyKind(request.DateOfBirth.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        return MapResult(await userService.SetBirthdayAsync(UserId, birthday));
    }

    [HttpPost("phone")]
    [SwaggerOperation(Summary = "Send OTP to a new phone number to confirm the change")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendPhoneChangeOtp([FromBody] ChangePhoneRequest request)
    {
        if (BlockIfScopedToken() is { } block) return block;
        if (UserId is null) return Forbid();

        if (await repository.GetByPhoneAsync(request.NewPhone, excludeUserId: UserId) is not null)
            return Conflict(new { message = "This phone number is already in use." });

        var limit = await rateLimiter.TryRecordAsync(OtpKeys.PhoneChange(UserId, request.NewPhone));
        if (!limit.Allowed)
            return StatusCode(429, new { message = "Please wait before requesting a new code.", retryAfterSeconds = limit.RetryAfterSeconds });

        var code = await otpService.GenerateAsync(OtpKeys.PhoneChange(UserId, request.NewPhone), OtpExpiry);
        await otpSender.SendSmsAsync(request.NewPhone, code);
        return Ok(new { message = "OTP sent.", retryAfterSeconds = limit.RetryAfterSeconds });
    }

    [HttpPatch("phone")]
    [SwaggerOperation(Summary = "Update phone number — requires OTP sent to the new number")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePhone([FromBody] VerifyPhoneChangeRequest request)
    {
        if (BlockIfScopedToken() is { } block) return block;
        if (UserId is null) return Forbid();

        if (await repository.GetByPhoneAsync(request.NewPhone, excludeUserId: UserId) is not null)
            return Conflict(new { message = "This phone number is already in use." });

        if (!await otpService.VerifyAsync(OtpKeys.PhoneChange(UserId, request.NewPhone), request.Code))
            return BadRequest(new { message = "Invalid or expired OTP." });

        return MapResult(await userService.SetVerifiedPhoneAsync(UserId, request.NewPhone));
    }

    [HttpPost("email")]
    [SwaggerOperation(Summary = "Send OTP to a new email address to confirm the change")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendEmailChangeOtp([FromBody] ChangeEmailRequest request)
    {
        if (BlockIfScopedToken() is { } block) return block;
        if (UserId is null) return Forbid();

        if (await repository.GetByEmailAsync(request.NewEmail) is { } existing && existing.Id != UserId)
            return Conflict(new { message = "This email address is already in use." });

        var limit = await rateLimiter.TryRecordAsync(OtpKeys.EmailChange(UserId, request.NewEmail));
        if (!limit.Allowed)
            return StatusCode(429, new { message = "Please wait before requesting a new code.", retryAfterSeconds = limit.RetryAfterSeconds });

        var code = await otpService.GenerateAsync(OtpKeys.EmailChange(UserId, request.NewEmail), OtpExpiry);
        await otpSender.SendEmailAsync(request.NewEmail, code);
        return Ok(new { message = "OTP sent.", retryAfterSeconds = limit.RetryAfterSeconds });
    }

    [HttpPatch("email")]
    [SwaggerOperation(Summary = "Update email address — requires OTP sent to the new address")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmail([FromBody] VerifyEmailChangeRequest request)
    {
        if (BlockIfScopedToken() is { } block) return block;
        if (UserId is null) return Forbid();

        if (await repository.GetByEmailAsync(request.NewEmail) is { } existing && existing.Id != UserId)
            return Conflict(new { message = "This email address is already in use." });

        if (!await otpService.VerifyAsync(OtpKeys.EmailChange(UserId, request.NewEmail), request.Code))
            return BadRequest(new { message = "Invalid or expired OTP." });

        return MapResult(await userService.SetVerifiedEmailAsync(UserId, request.NewEmail));
    }

    [HttpPost("password")]
    [Authorize(Policy = TandurPolicies.AdminPanel)]
    [SwaggerOperation(Summary = "Send password reset token to registered email (admin only)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendPasswordResetToken()
    {
        if (BlockIfScopedToken() is { } block) return block;
        if (UserId is null) return Forbid();

        var user = await userManager.FindByIdAsync(UserId);
        if (user is null) return NotFound();

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await passwordResetSender.SendAsync(user.Email!, token);
        return Ok(new { message = "Password reset link sent to your email." });
    }

    [HttpPatch("password")]
    [Authorize(Policy = TandurPolicies.AdminPanel)]
    [SwaggerOperation(Summary = "Reset password using the token sent to email (admin only)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithTokenRequest request)
    {
        if (BlockIfScopedToken() is { } block) return block;
        if (UserId is null) return Forbid();

        var user = await userManager.FindByIdAsync(UserId);
        if (user is null) return NotFound();

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await refreshTokenService.RevokeAllForUserAsync(UserId);
        return NoContent();
    }

    [HttpPost("delete")]
    [SwaggerOperation(Summary = "Send OTP to registered phone to confirm account deletion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendDeleteOtp()
    {
        if (BlockIfScopedToken() is { } block) return block;
        if (UserId is null) return Forbid();

        var user = await userManager.FindByIdAsync(UserId);
        if (user is null) return NotFound();
        if (user.PhoneNumber is null)
            return BadRequest(new { message = "No phone number on file." });

        var limit = await rateLimiter.TryRecordAsync(OtpKeys.AccountDelete(UserId));
        if (!limit.Allowed)
            return StatusCode(429, new { message = "Please wait before requesting a new code.", retryAfterSeconds = limit.RetryAfterSeconds });

        var code = await otpService.GenerateAsync(OtpKeys.AccountDelete(UserId), OtpExpiry);
        await otpSender.SendSmsAsync(user.PhoneNumber, code);
        return Ok(new { message = "OTP sent.", retryAfterSeconds = limit.RetryAfterSeconds });
    }

    [HttpDelete]
    [SwaggerOperation(Summary = "Delete own account — requires OTP confirmation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        if (BlockIfScopedToken() is { } block) return block;
        if (UserId is null) return Forbid();

        if (!await otpService.VerifyAsync(OtpKeys.AccountDelete(UserId), request.Code))
            return BadRequest(new { message = "Invalid or expired OTP." });

        var user = await userManager.FindByIdAsync(UserId);
        if (user is null) return NotFound();

        await refreshTokenService.RevokeAllForUserAsync(UserId);
        var result = await userManager.DeleteAsync(user);
        return result.Succeeded
            ? NoContent()
            : BadRequest(new { errors = result.Errors.Select(e => e.Description) });
    }

    private IActionResult MapResult(UserUpdateResult result) => result switch
    {
        UserUpdateResult.Success  => NoContent(),
        UserUpdateResult.NotFound => NotFound(),
        UserUpdateResult.Failed f => BadRequest(new { errors = f.Errors }),
        _                        => StatusCode(500),
    };
}
