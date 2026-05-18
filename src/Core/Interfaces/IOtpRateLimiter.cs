namespace Core.Interfaces;

public record RateLimitResult(bool Allowed, int RetryAfterSeconds);

public interface IOtpRateLimiter
{
    Task<RateLimitResult> TryRecordAsync(string key);
}
