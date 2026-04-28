using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Services;

public class AuthCookieService : IAuthCookieService
{
    private const string AccessTokenCookie = "access_token";
    private const string RefreshTokenCookie = "refresh_token";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JwtSettings _jwtSettings;

    public AuthCookieService(IHttpContextAccessor httpContextAccessor, IOptions<JwtSettings> jwtOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _jwtSettings = jwtOptions.Value;
    }

    public void SetAuthCookies(string accessToken, string refreshToken)
    {
        var response = _httpContextAccessor.HttpContext!.Response;
        var isProduction = !_httpContextAccessor.HttpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);

        response.Cookies.Append(AccessTokenCookie, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes)
        });

        response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            Path = "/api/auth/refresh"
        });
    }

    public void DeleteAuthCookies()
    {
        var response = _httpContextAccessor.HttpContext!.Response;
        response.Cookies.Delete(AccessTokenCookie);
        response.Cookies.Delete(RefreshTokenCookie, new CookieOptions { Path = "/api/auth/refresh" });
    }

    public string? GetRefreshToken() =>
        _httpContextAccessor.HttpContext?.Request.Cookies[RefreshTokenCookie];
}
