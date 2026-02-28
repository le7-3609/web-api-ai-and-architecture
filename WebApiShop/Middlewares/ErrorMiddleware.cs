using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace WebApiShop
{
    public class ErrorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorMiddleware> _logger;

        public ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }catch (Exception ex)
            {
                _logger.LogError($"Something went wrong: {ex}");
                httpContext.Response.StatusCode = 500;
                await httpContext.Response.WriteAsync("Internal Server Error from the custom middleware.");
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ErrorMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorMiddleware>();
        }
    }
}
