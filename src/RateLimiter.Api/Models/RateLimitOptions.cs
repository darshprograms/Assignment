
namespace RateLimiter.Api.Models;

public class RateLimitOptions
{
    public const string RateLimiting = "RateLimiting";

    /// <summary>
    /// The default rule applied if no specific rule matches the key.
    /// </summary>
    public RateLimitRule DefaultRule { get; set; } = new RateLimitRule();

    /// <summary>
    /// Specific rules for specific keys (e.g. userId, clientId, or "group:name").
    /// </summary>
    public Dictionary<string, RateLimitRule> Rules { get; set; } = new Dictionary<string, RateLimitRule>();
}

public class RateLimitRule
{
    /// <summary>
    /// Number of requests allowed in the time window.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// The time window in seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
}
