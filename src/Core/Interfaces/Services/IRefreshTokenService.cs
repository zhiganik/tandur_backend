namespace Core.Interfaces.Services;

public interface IRefreshTokenService
{
    Task<string> CreateAsync(string userId, TimeSpan expiry);
    Task<string?> GetUserIdAsync(string token);
    Task RevokeAsync(string token);
    Task RevokeAllForUserAsync(string userId);
}
