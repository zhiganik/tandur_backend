namespace Core.Interfaces.Services;

public record OtpSession(string Phone, string? Email = null);

public interface IOtpSessionService
{
    Task<string> CreateAsync(string phone, TimeSpan expiry);
    Task<string?> GetPhoneAsync(string token);
    Task<string> CreateEmailVerifiedAsync(string phone, string email, TimeSpan expiry);
    Task<OtpSession?> GetSessionAsync(string token);
    Task InvalidateAsync(string token);
}
