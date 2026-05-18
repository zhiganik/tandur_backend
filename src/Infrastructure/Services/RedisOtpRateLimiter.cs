using Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class RedisOtpRateLimiter(IDistributedCache cache) : IOtpRateLimiter
{
    private const int CooldownSeconds     = 60;
    private const int MaxAttemptsPerHour  = 5;
    private const int HourSeconds         = 3600;

    private static string CooldownKey(string key) => $"otp_cd:{key}";
    private static string CountKey(string key)    => $"otp_cnt:{key}";

    public async Task<RateLimitResult> TryRecordAsync(string key)
    {
        var now = DateTimeOffset.UtcNow;

        // Check per-send cooldown
        var cooldownValue = await cache.GetStringAsync(CooldownKey(key));
        if (cooldownValue is not null)
        {
            var expiresAt       = DateTimeOffset.FromUnixTimeSeconds(long.Parse(cooldownValue));
            var remainingSeconds = Math.Max(1, (int)(expiresAt - now).TotalSeconds);
            return new RateLimitResult(false, remainingSeconds);
        }

        // Check hourly cap
        var (count, hourlyExpiry) = await GetCountAsync(key, now);
        if (count >= MaxAttemptsPerHour)
        {
            var remainingSeconds = Math.Max(1, (int)(hourlyExpiry - now).TotalSeconds);
            return new RateLimitResult(false, remainingSeconds);
        }

        // Record: set cooldown + increment hourly count
        var cooldownExpiry = now.AddSeconds(CooldownSeconds);
        await cache.SetStringAsync(CooldownKey(key), cooldownExpiry.ToUnixTimeSeconds().ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CooldownSeconds) });

        var newExpiry = hourlyExpiry == default ? now.AddSeconds(HourSeconds) : hourlyExpiry;
        await cache.SetStringAsync(CountKey(key), FormatCount(count + 1, newExpiry),
            new DistributedCacheEntryOptions { AbsoluteExpiration = newExpiry });

        return new RateLimitResult(true, CooldownSeconds);
    }

    private async Task<(int Count, DateTimeOffset Expiry)> GetCountAsync(string key, DateTimeOffset now)
    {
        var stored = await cache.GetStringAsync(CountKey(key));
        if (stored is null) return (0, default);

        var parts = stored.Split('|');
        if (parts.Length != 2) return (0, default);

        var count    = int.TryParse(parts[0], out var c) ? c : 0;
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.TryParse(parts[1], out var ts) ? ts : 0);
        return (count, expiresAt > now ? expiresAt : default);
    }

    private static string FormatCount(int count, DateTimeOffset expiry) =>
        $"{count}|{expiry.ToUnixTimeSeconds()}";
}
