
using Microsoft.AspNetCore.Mvc;
using RateLimiter.Api.Models;
using RateLimiter.Api.Services;

namespace RateLimiter.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class RateLimiterController : ControllerBase
{
    private readonly IRateLimiter _rateLimiter;

    public RateLimiterController(IRateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter;
    }

    [HttpPost("check")]
    public async Task<IActionResult> Check([FromBody] CheckRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserId))
        {
            return BadRequest(new { Message = "UserId is required." });
        }

        var result = await _rateLimiter.CheckAsync(request.UserId);

        if (result.IsAllowed)
        {
            return Ok(new { Message = "Allowed", RemainingPermits = result.RemainingPermits });
        }
        else
        {
            if (result.ResetTime.HasValue)
            {
                // RFC 1123 format for Retry-After header
                Response.Headers["Retry-After"] = result.ResetTime.Value.ToString("R");
            }
            
            return StatusCode(429, new 
            { 
                Message = "Too Many Requests", 
                RetryAfter = result.ResetTime 
            });
        }
    }
}

public class CheckRequest
{
    public string UserId { get; set; } = string.Empty;
}
