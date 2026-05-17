namespace Core.Interfaces;

public interface IRefreshTokenService
{
    Task<string> CreateAsync(string userId, TimeSpan expiry, string clientType);
    Task<string?> GetUserIdAsync(string token);
    Task RevokeAsync(string token);
    Task RevokeAllForUserAsync(string userId);
}
