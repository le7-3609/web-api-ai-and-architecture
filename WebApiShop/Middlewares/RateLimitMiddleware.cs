using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApiShop;

public static class RateLimitMiddleware
{
    public const string PolicyName = "SpecificPolicy";

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/json";

            var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                ? Math.Ceiling(retryAfterValue.TotalSeconds)
                : 60;

            var response = new
            {
                message = "Rate limit exceeded. You can send up to 100 requests per minute.",
                policy = PolicyName,
                retryAfterSeconds = retryAfter
            };

            await context.HttpContext.Response.WriteAsync(
                JsonSerializer.Serialize(response),
                cancellationToken);
        };

        options.AddPolicy(PolicyName, httpContext =>
        {
            var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
    }
}