# Rate Limiter Service

A simplified, in-memory rate-limiting service built with .NET 10.

## Features

- **Rate Limiting Endpoint**: `POST /RateLimiter/check`
- **Configurable Rules**: Modify rate limits in `appsettings.json` without code changes.
- **In-Memory Storage**: High-performance in-memory state management.
- **Token Bucket Algorithm**: Handles bursts and maintains average rate.

## Getting Started

### Prerequisites

- .NET 10.0 SDK

### Running the Service

1. Navigate to the solution root:
   ```bash
   cd c:\Users\darakadk\Desktop\Interview_Assignment\Assignment
   ```
2. Run the application:
   ```bash
   dotnet run --project src/RateLimiter.Api
   ```
3. The service will start (check console for URL, typically `http://localhost:5032`).

### Usage

**Check Request Limit**

```http
POST /RateLimiter/check
Content-Type: application/json

{
    "userId": "user123"
}
```

**Response (Allowed)**
```json
{
    "message": "Allowed",
    "remainingPermits": 99
}
```

**Response (Blocked)**
```json
{
    "message": "Too Many Requests",
    "retryAfter": "2026-01-09T12:01:00Z"
}
```

## Integration Guide: How to Use in Another Service (e.g., Booking Service)

To use the Rate Limiter from another microservice (like a Booking Service), you need to make an HTTP call to the `/RateLimiter/check` endpoint before processing a user's request.

### Steps

1.  **Intercept the Request**: In your service (e.g., at the Controller or Middleware level), extract the `userId` or identifying key.
2.  **Call Rate Limiter**: Send a POST request to the Rate Limiter service.
3.  **Handle Response**:
    *   **200 OK**: Proceed with your logic.
    *   **429 Too Many Requests**: Reject the request and optionally return the `Retry-After` header to the user.

### Example C# Implementation

Here is a helper class you can add to your **Booking Service**:

```csharp
public class RateLimiterClient
{
    private readonly HttpClient _httpClient;

    public RateLimiterClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsAllowedAsync(string userId)
    {
        var response = await _httpClient.PostAsJsonAsync("http://localhost:5291/RateLimiter/check", new { UserId = userId });

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            // Optional: Read Retry-After header if you want to expose it
            // var retryAfter = response.Headers.RetryAfter.Date;
            return false;
        }

        // Handle other errors (fail open or closed depending on policy)
        return true; 
    }
}
```

### Usage in Controller

```csharp
[HttpPost("book")]
public async Task<IActionResult> CreateBooking([FromBody] BookingRequest request)
{
    // 1. Check Rate Limit
    if (!await _rateLimiterClient.IsAllowedAsync(request.UserId))
    {
        return StatusCode(429, "Rate limit exceeded. Please try again later.");
    }

    // 2. Proceed with Booking Logic
    _bookingService.Book(request);
    
    return Ok();
}
```

## Configuration

Edit `src/RateLimiter.Api/appsettings.json`:

```json
"RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60
}
```
- `PermitLimit`: Number of requests allowed in the window.
- `WindowSeconds`: The time window in seconds.

## Design Decisions

### Algorithm: Token Bucket
I chose the **Token Bucket** algorithm for this implementation.

**Justification:**
1.  **Burst Handling**: Unlike a Fixed Window algorithm, Token Bucket allows for bursts of traffic up to the bucket capacity while still enforcing an average rate over time. This provides a better user experience for real-world usage patterns.
2.  **Efficiency**: It requires storing only two values per user (Token Count and Last Refill Time), making it memory efficient. Refill calculations are done lazily on request, avoiding background timers.
3.  **Correctness**: It avoids the "double limit" boundary issue found in simple Fixed Window counters.

### Storage: In-Memory with Swappability
The current implementation uses a `ConcurrentDictionary` and `lock` mechanism for thread safety. The architecture is designed with the `IRateLimiter` interface, allowing the backing store to be easily swapped for a distributed store like Redis (using Lua scripts for atomicity) without changing the controller or business logic contracts.

### Dependency Injection
- `IRateLimiter` is registered as a Singleton because it maintains the in-memory state.
- `IClock` is abstracted to allow deterministic unit testing of time-based logic.

## Future Improvements
- **Distributed Storage**: Implement a Redis-backed `IRateLimiter` for horizontal scaling.
- **Multiple Rules**: Support different limits for different user tiers or API keys.
- **Middleware**: Implement as ASP.NET Core Middleware for transparent request filtering.