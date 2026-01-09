using RateLimiter.Api.Models;
using RateLimiter.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register Configuration
builder.Services.Configure<RateLimitOptions>(
    builder.Configuration.GetSection(RateLimitOptions.RateLimiting));

builder.Services.AddSingleton<IClock, SystemClock>();

// Register RateLimiter service as Singleton because it holds in-memory state.
builder.Services.AddSingleton<IRateLimiter, InMemoryTokenBucketRateLimiter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
