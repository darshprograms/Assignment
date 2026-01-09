
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using RateLimiter.Api.Models;

namespace RateLimiter.Api.Services;

/// <summary>
/// Implements the Token Bucket algorithm for rate limiting.
/// This implementation stores state in-memory but is designed behind the IRateLimiter interface
/// so it can be swapped for a distributed implementation (e.g., Redis) later.
/// </summary>
public class InMemoryTokenBucketRateLimiter : IRateLimiter
{
    private readonly IOptionsMonitor<RateLimitOptions> _options;
    private readonly IClock _clock;
    
    // In-memory store. For a distributed solution, this would be replaced by a cache client (Redis).
    private readonly ConcurrentDictionary<string, Bucket> _buckets = new();

    public InMemoryTokenBucketRateLimiter(IOptionsMonitor<RateLimitOptions> options, IClock clock)
    {
        _options = options;
        _clock = clock;
    }

    public Task<RateLimitResult> CheckAsync(string key)
    {
        var rules = _options.CurrentValue;
        
        // Ensure rules are sane to avoid division by zero
        if (rules.PermitLimit <= 0 || rules.WindowSeconds <= 0)
        {
            // Fail safe or allow? Let's assume misconfig blocks or allows. 
            // Blocking is safer for protection.
            return Task.FromResult(new RateLimitResult(false, 0, null));
        }
    
        // Get the user's bucket or create a new one with full capacity
        var bucket = _buckets.GetOrAdd(key, _ => new Bucket(rules.PermitLimit, _clock.UtcNow));

        // Lock to ensure atomicity of the "refill and consume" operation.
        // In a distributed system (Redis), this would be a Lua script.
        lock (bucket)
        {
            var now = _clock.UtcNow;
            var timeElapsed = (now - bucket.LastRefill).TotalSeconds;

            if (timeElapsed < 0) timeElapsed = 0; // Clock skew protection

            var refillRate = rules.PermitLimit / (double)rules.WindowSeconds;
            var tokensToAdd = timeElapsed * refillRate;

            // Refill tokens, up to the max burst capacity (PermitLimit)
            bucket.Tokens = Math.Min(rules.PermitLimit, bucket.Tokens + tokensToAdd);
            bucket.LastRefill = now;

            if (bucket.Tokens >= 1.0)
            {
                bucket.Tokens -= 1.0;
                return Task.FromResult(new RateLimitResult(true, (int)bucket.Tokens, null));
            }
            else
            {
                // Calculate when enough tokens will be available for 1 request
                // We need 1.0 token. We have bucket.Tokens.
                // Missing = 1.0 - bucket.Tokens
                // Time = Missing / Rate
                var missingTokens = 1.0 - bucket.Tokens;
                var timeToWaitSeconds = missingTokens / refillRate;
                var resetTime = now.AddSeconds(timeToWaitSeconds);

                return Task.FromResult(new RateLimitResult(false, (int)bucket.Tokens, resetTime));
            }
        }
    }

    // Internal class to hold state
    private class Bucket
    {
        public double Tokens { get; set; }
        public DateTime LastRefill { get; set; }

        public Bucket(double tokens, DateTime lastRefill)
        {
            Tokens = tokens;
            LastRefill = lastRefill;
        }
    }
}
