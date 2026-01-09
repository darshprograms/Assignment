
namespace RateLimiter.Api.Models;

public class RateLimitOptions
{
    public const string RateLimiting = "RateLimiting";

    /// <summary>
    ///  Number of requests allowed in the time window.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// The time window in seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;
}
