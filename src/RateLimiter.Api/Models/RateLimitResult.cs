
namespace RateLimiter.Api.Models;

public record RateLimitResult(bool IsAllowed, int RemainingPermits, DateTime? ResetTime);
