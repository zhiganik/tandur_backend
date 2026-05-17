using System.Security.Cryptography;
using Core.Domain.Constants;
using Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class RedisRefreshTokenService(IDistributedCache cache) : IRefreshTokenService
{
    private static string TokenKey(string token) => $"refresh:{token}";
    private static string UserKey(string userId, string clientType) => $"user_refresh:{clientType}:{userId}";

    public async Task<string> CreateAsync(string userId, TimeSpan expiry, string clientType)
    {
        var existingToken = await cache.GetStringAsync(UserKey(userId, clientType));
        if (existingToken is not null)
            await cache.RemoveAsync(TokenKey(existingToken));

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry };

        await cache.SetStringAsync(TokenKey(token), $"{userId}:{clientType}", options);
        await cache.SetStringAsync(UserKey(userId, clientType), token, options);

        return token;
    }

    public async Task<string?> GetUserIdAsync(string token)
    {
        var value = await cache.GetStringAsync(TokenKey(token));
        if (value is null) return null;
        var colonIndex = value.IndexOf(':');
        return colonIndex > 0 ? value[..colonIndex] : value;
    }

    public async Task RevokeAsync(string token)
    {
        var value = await cache.GetStringAsync(TokenKey(token));
        if (value is not null)
        {
            var parts = value.Split(':', 2);
            var userId = parts[0];
            var clientType = parts.Length > 1 ? parts[1] : ClientTypes.Mobile;
            await cache.RemoveAsync(UserKey(userId, clientType));
        }
        await cache.RemoveAsync(TokenKey(token));
    }

    public async Task RevokeAllForUserAsync(string userId)
    {
        foreach (var clientType in new[] { ClientTypes.Web, ClientTypes.Mobile })
        {
            var token = await cache.GetStringAsync(UserKey(userId, clientType));
            if (token is not null)
                await cache.RemoveAsync(TokenKey(token));
            await cache.RemoveAsync(UserKey(userId, clientType));
        }
    }
}
