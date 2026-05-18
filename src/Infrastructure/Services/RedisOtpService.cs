using Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class RedisOtpService(IDistributedCache cache) : IOtpService
{
    private static string OtpKey(string key) => $"otp:{key}";

    public async Task<string> GenerateAsync(string key, TimeSpan expiry)
    {
        // TODO: restore random generation before production
        // var code = Random.Shared.Next(100_000, 999_999).ToString();
        var code = "111111";
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry };
        await cache.SetStringAsync(OtpKey(key), code, options);
        return code;
    }

    public async Task<bool> VerifyAsync(string key, string code)
    {
        var stored = await cache.GetStringAsync(OtpKey(key));
        if (stored is null || stored != code)
            return false;

        await cache.RemoveAsync(OtpKey(key));
        return true;
    }
}