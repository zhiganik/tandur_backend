namespace Core.Interfaces.Services;

public interface IOtpService
{
    Task<string> GenerateAsync(string key, TimeSpan expiry);
    Task<bool> VerifyAsync(string key, string code);
}
