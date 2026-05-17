using System.Security.Cryptography;
using System.Text.Json;
using Core.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class RedisOtpSessionService(IDistributedCache cache) : IOtpSessionService
{
    private static string SessionKey(string token) => $"otp_session:{token}";

    public async Task<string> CreateAsync(string phone, TimeSpan expiry)
    {
        var token = NewToken();
        await SetAsync(token, new OtpSession(phone), expiry);
        return token;
    }

    public async Task<string?> GetPhoneAsync(string token)
    {
        var session = await GetSessionAsync(token);
        return session?.Phone;
    }

    public async Task<string> CreateEmailVerifiedAsync(string phone, string email, TimeSpan expiry)
    {
        var token = NewToken();
        await SetAsync(token, new OtpSession(phone, email), expiry);
        return token;
    }

    public async Task<OtpSession?> GetSessionAsync(string token)
    {
        var raw = await cache.GetStringAsync(SessionKey(token));
        return raw is null ? null : JsonSerializer.Deserialize<OtpSession>(raw);
    }

    public Task InvalidateAsync(string token) =>
        cache.RemoveAsync(SessionKey(token));

    private Task SetAsync(string token, OtpSession session, TimeSpan expiry) =>
        cache.SetStringAsync(SessionKey(token), JsonSerializer.Serialize(session),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry });

    private static string NewToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
}