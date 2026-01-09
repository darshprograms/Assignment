
using RateLimiter.Api.Models;

namespace RateLimiter.Api.Services;

public interface IRateLimiter
{
    Task<RateLimitResult> CheckAsync(string key);
}
