using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Services;

namespace WebApiShop;

/// <summary>
/// Intercepts every HTTP request, extracts the JWT from the "access_token" HttpOnly cookie,
/// validates it via IJwtService, and populates HttpContext.User so that downstream middleware
/// (UseAuthorization) and controller actions can rely on an authenticated ClaimsPrincipal.
///
/// IJwtService is injected via method injection (InvokeAsync parameter) to correctly handle
/// the scoped lifetime of the service within this singleton middleware.
/// </summary>
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
