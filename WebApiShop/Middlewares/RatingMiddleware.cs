using Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Services;
using System.Threading.Tasks;

namespace WebApiShop
{
    public class RatingMiddleware
    {
        private readonly RequestDelegate _next;

        public RatingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, IRatingService ratingService)
        {
            Rating rating = new Rating
            {
                Host = httpContext.Request.Host.HasValue ? httpContext.Request.Host.Value : string.Empty,
                Method = httpContext.Request.Method,
                Path = httpContext.Request.Path.HasValue ? httpContext.Request.Path.Value : string.Empty,
                Referer = httpContext.Request.Headers.Referer.ToString(),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                RecordDate = DateTime.UtcNow
            };
            await ratingService.AddRatingAsync(rating);
            await _next(httpContext);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseRatingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RatingMiddleware>();
        }
    }
}
