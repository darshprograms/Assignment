
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RateLimiter.Api.Models;
using RateLimiter.Api.Services;
using Xunit;

namespace RateLimiter.Tests;

public class TokenBucketRateLimiterTests
{
    private readonly Mock<IOptionsMonitor<RateLimitOptions>> _mockOptions;
    private readonly Mock<IClock> _mockClock;
    private readonly Mock<ILogger<InMemoryTokenBucketRateLimiter>> _mockLogger;
    private readonly InMemoryTokenBucketRateLimiter _rateLimiter;
    private readonly RateLimitOptions _options;

    public TokenBucketRateLimiterTests()
    {
        _options = new RateLimitOptions { PermitLimit = 10, WindowSeconds = 60 };
        _mockOptions = new Mock<IOptionsMonitor<RateLimitOptions>>();
        _mockOptions.Setup(o => o.CurrentValue).Returns(_options);
        
        _mockClock = new Mock<IClock>();
        _mockClock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        _mockLogger = new Mock<ILogger<InMemoryTokenBucketRateLimiter>>();

        _rateLimiter = new InMemoryTokenBucketRateLimiter(
            _mockOptions.Object, 
            _mockClock.Object, 
            _mockLogger.Object);
    }

    [Fact]
    public async Task CheckAsync_AllowsRequest_WhenWithinLimit()
    {
        // Act
        var result = await _rateLimiter.CheckAsync("user1");

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal(9, result.RemainingPermits);
    }

    [Fact]
    public async Task CheckAsync_BlocksRequest_WhenLimitExceeded()
    {
        // Arrange
        _options.PermitLimit = 1;

        // Act - Consume 1
        var result1 = await _rateLimiter.CheckAsync("user2");
        
        // Act - Consume 2 (should fail)
        var result2 = await _rateLimiter.CheckAsync("user2");

        // Assert
        Assert.True(result1.IsAllowed);
        Assert.False(result2.IsAllowed);
        Assert.Equal(0, result2.RemainingPermits);
        Assert.NotNull(result2.ResetTime);
    }
    
    [Fact]
    public async Task CheckAsync_RefillsTokens_OverTime()
    {
        // Arrange
        // Limit 10 per 1 second
        _options.PermitLimit = 10;
        _options.WindowSeconds = 1; // refill rate = 10 per second
        
        // Current Time: 12:00:00
        
        // Consume all 10
        for (int i = 0; i < 10; i++)
        {
            await _rateLimiter.CheckAsync("user3");
        }
        
        var emptyResult = await _rateLimiter.CheckAsync("user3");
        Assert.False(emptyResult.IsAllowed);

        // Act - Advance time by 0.5 seconds -> should refill 5 tokens
        _mockClock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 1, 1, 12, 0, 0, 500, DateTimeKind.Utc));

        var refillResult = await _rateLimiter.CheckAsync("user3");

        // Assert
        Assert.True(refillResult.IsAllowed);
        // We had 0. Refilled 5. Consumed 1. Remaining 4.
        Assert.Equal(4, refillResult.RemainingPermits);
    }
}
