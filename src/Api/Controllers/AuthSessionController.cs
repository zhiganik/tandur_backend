using Core.Domain.Constants;
using Core.DTOs.Auth;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Api.Controllers;

[ApiController]
[Route("api/auth/session")]
[Tags("Auth › Session")]
[Produces("application/json")]
public class AuthSessionController(
    IOtpService otpService,
    IOtpSender otpSender,
    IOtpSessionService otpSessionService,
    IOtpRateLimiter rateLimiter) : ControllerBase
{
    private static readonly TimeSpan PhoneOtpExpiry = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan EmailOtpExpiry = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan SessionExpiry  = TimeSpan.FromMinutes(15);

    [HttpPost("phone")]
    [SwaggerOperation(Summary = "Send OTP to a phone number")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SendPhoneOtp([FromBody] SendPhoneOtpRequest request)
    {
        var limit = await rateLimiter.TryRecordAsync(OtpKeys.Phone(request.PhoneNumber));
        if (!limit.Allowed)
            return StatusCode(429, new { message = "Please wait before requesting a new code.", retryAfterSeconds = limit.RetryAfterSeconds });

        var code = await otpService.GenerateAsync(OtpKeys.Phone(request.PhoneNumber), PhoneOtpExpiry);
        await otpSender.SendSmsAsync(request.PhoneNumber, code);
        return Ok(new { message = "OTP sent.", retryAfterSeconds = limit.RetryAfterSeconds });
    }

    [HttpPost("phone/verify")]
    [SwaggerOperation(Summary = "Verify phone OTP — returns a session token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyPhoneOtp([FromBody] VerifyPhoneOtpRequest request)
    {
        if (!await otpService.VerifyAsync(OtpKeys.Phone(request.PhoneNumber), request.Code))
            return BadRequest(new { message = "Invalid or expired OTP." });

        var sessionToken = await otpSessionService.CreateAsync(request.PhoneNumber, SessionExpiry);
        return Ok(new { sessionToken });
    }

    [HttpPost("email")]
    [SwaggerOperation(Summary = "Send OTP to email (requires active phone session)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> SendEmailOtp([FromBody] SendEmailOtpRequest request)
    {
        var phone = await otpSessionService.GetPhoneAsync(request.SessionToken);
        if (phone is null)
            return Unauthorized(new { message = "Session expired. Please verify your phone again." });

        var limit = await rateLimiter.TryRecordAsync(OtpKeys.Email(request.Email));
        if (!limit.Allowed)
            return StatusCode(429, new { message = "Please wait before requesting a new code.", retryAfterSeconds = limit.RetryAfterSeconds });

        var code = await otpService.GenerateAsync(OtpKeys.Email(request.Email), EmailOtpExpiry);
        await otpSender.SendEmailAsync(request.Email, code);
        return Ok(new { message = "OTP sent.", retryAfterSeconds = limit.RetryAfterSeconds });
    }

    [HttpPost("email/verify")]
    [SwaggerOperation(Summary = "Verify email OTP — upgrades session token (phone + email both confirmed)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyEmailOtpRequest request)
    {
        var phone = await otpSessionService.GetPhoneAsync(request.SessionToken);
        if (phone is null)
            return Unauthorized(new { message = "Session expired. Please verify your phone again." });

        if (!await otpService.VerifyAsync(OtpKeys.Email(request.Email), request.Code))
            return BadRequest(new { message = "Invalid or expired OTP." });

        var sessionToken = await otpSessionService.CreateEmailVerifiedAsync(phone, request.Email, SessionExpiry);
        return Ok(new { sessionToken });
    }
}
