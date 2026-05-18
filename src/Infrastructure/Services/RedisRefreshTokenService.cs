using System.Security.Cryptography;
using Core.Domain.Enums;
using Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public class RedisRefreshTokenService(IDistributedCache cache, IConfiguration config) : IRefreshTokenService
{
    private static string TokenKey(string token)              => $"refresh:{token}";
    private static string UserKey(string userId, string type) => $"user_refresh:{type}:{userId}";

    private TimeSpan ExpiryFor(ClientType clientType) => clientType switch
    {
        ClientType.Web    => TimeSpan.FromDays(int.Parse(config["Jwt:AdminRefreshExpiryDays"] ?? "2")),
        ClientType.Mobile => TimeSpan.FromDays(int.Parse(config["Jwt:RefreshExpiryDays"] ?? "30")),
        _                 => TimeSpan.FromDays(30),
    };

    public async Task<string> CreateAsync(string userId, ClientType clientType)
    {
        var typeKey = clientType.ToString().ToLowerInvariant();
        var existingToken = await cache.GetStringAsync(UserKey(userId, typeKey));
        if (existingToken is not null)
            await cache.RemoveAsync(TokenKey(existingToken));

        var token   = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var expiry  = ExpiryFor(clientType);
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry };

        await cache.SetStringAsync(TokenKey(token), $"{userId}:{typeKey}", options);
        await cache.SetStringAsync(UserKey(userId, typeKey), token, options);

        return token;
    }

    public async Task<(string? UserId, ClientType? Type)> GetAsync(string token)
    {
        var value = await cache.GetStringAsync(TokenKey(token));
        if (value is null) return (null, null);

        var parts      = value.Split(':', 2);
        var userId     = parts[0];
        ClientType? ct = parts.Length > 1 && Enum.TryParse<ClientType>(parts[1], ignoreCase: true, out var parsed)
            ? parsed
            : null;

        return (userId, ct);
    }

    public async Task RevokeAsync(string token)
    {
        var value = await cache.GetStringAsync(TokenKey(token));
        if (value is not null)
        {
            var parts   = value.Split(':', 2);
            var userId  = parts[0];
            var typeKey = parts.Length > 1 ? parts[1] : "mobile";
            await cache.RemoveAsync(UserKey(userId, typeKey));
        }
        await cache.RemoveAsync(TokenKey(token));
    }

    public async Task RevokeAllForUserAsync(string userId)
    {
        foreach (var clientType in Enum.GetValues<ClientType>())
        {
            var typeKey = clientType.ToString().ToLowerInvariant();
            var token   = await cache.GetStringAsync(UserKey(userId, typeKey));
            if (token is not null)
                await cache.RemoveAsync(TokenKey(token));
            await cache.RemoveAsync(UserKey(userId, typeKey));
        }
    }
}
