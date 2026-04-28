using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Services;

namespace WebApiShop;
public class JwtMiddleware
{
    private const string AccessTokenCookie = "access_token";
    private readonly RequestDelegate _next;

    public JwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
    {
        var token = context.Request.Cookies[AccessTokenCookie];

        if (!string.IsNullOrWhiteSpace(token))
        {
            var principal = await jwtService.ValidateAccessTokenAsync(token);
            if (principal != null)
                context.User = principal;
        }

        await _next(context);
    }
}

public static class JwtMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder builder)
        => builder.UseMiddleware<JwtMiddleware>();
}
