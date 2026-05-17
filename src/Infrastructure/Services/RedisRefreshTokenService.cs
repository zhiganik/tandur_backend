using System.Security.Cryptography;
using Core.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class RedisRefreshTokenService(IDistributedCache cache) : IRefreshTokenService
{
    private static string TokenKey(string token) => $"refresh:{token}";
    private static string UserKey(string userId) => $"user_refresh:{userId}";

    public async Task<string> CreateAsync(string userId, TimeSpan expiry)
    {
        // revoke any existing token for this user (one active token per user)
        var existingToken = await cache.GetStringAsync(UserKey(userId));
        if (existingToken is not null)
            await cache.RemoveAsync(TokenKey(existingToken));

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry };

        await cache.SetStringAsync(TokenKey(token), userId, options);
        await cache.SetStringAsync(UserKey(userId), token, options);

        return token;
    }

    public Task<string?> GetUserIdAsync(string token) =>
        cache.GetStringAsync(TokenKey(token))!;

    public async Task RevokeAsync(string token)
    {
        var userId = await cache.GetStringAsync(TokenKey(token));
        if (userId is not null)
            await cache.RemoveAsync(UserKey(userId));
        await cache.RemoveAsync(TokenKey(token));
    }

    public async Task RevokeAllForUserAsync(string userId)
    {
        var token = await cache.GetStringAsync(UserKey(userId));
        if (token is not null)
            await cache.RemoveAsync(TokenKey(token));
        await cache.RemoveAsync(UserKey(userId));
    }
}