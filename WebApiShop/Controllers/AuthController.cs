using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Services;
using System.Security.Claims;

namespace WebApiShop.Controllers;


[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private const string AccessTokenCookie = "access_token";
    private const string RefreshTokenCookie = "refresh_token";

    private readonly IAuthService _authService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(IAuthService authService, IOptions<JwtSettings> jwtOptions)
    {
        _authService = authService;
        _jwtSettings = jwtOptions.Value;
    }

    // POST api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDTO>> LoginAsync([FromBody] LoginDTO dto)
    {
        var (result, error) = await _authService.LoginAsync(dto);
        if (result == null) return Unauthorized(error);

        SetAuthCookies(result.AccessToken, result.RefreshToken);
        return Ok(result.UserInfo);
    }

    // POST api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDTO>> RegisterAsync([FromBody] RegisterDTO dto)
    {
        var (result, error) = await _authService.RegisterAsync(dto);
        if (result == null) return BadRequest(error);

        SetAuthCookies(result.AccessToken, result.RefreshToken);
        return CreatedAtAction(nameof(LoginAsync), result.UserInfo);
    }

    // POST api/auth/refresh
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDTO>> RefreshAsync()
    {
        var rawRefreshToken = Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            return Unauthorized("Refresh token not found.");

        var result = await _authService.RefreshAsync(rawRefreshToken);
        if (result == null)
            return Unauthorized("Invalid or expired refresh token.");

        SetAuthCookies(result.AccessToken, result.RefreshToken);
        return Ok(result.UserInfo);
    }

    // POST api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> LogoutAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(userIdClaim, out var userId))
            await _authService.LogoutAsync(userId);

        DeleteAuthCookies();
        return NoContent();
    }

    // POST api/auth/social-login
    [HttpPost("social-login")]
    public async Task<ActionResult<AuthResponseDTO>> SocialLoginAsync([FromBody] SocialLoginDTO dto)
    {
        var (result, error) = await _authService.SocialLoginAsync(dto);
        if (result == null) return Unauthorized(error);

        SetAuthCookies(result.AccessToken, result.RefreshToken);
        return Ok(result.UserInfo);
    }


    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        Response.Cookies.Append(AccessTokenCookie, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes)
        });

        Response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            Path = "/api/auth/refresh"
        });
    }

    private void DeleteAuthCookies()
    {
        Response.Cookies.Delete(AccessTokenCookie);
        Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions { Path = "/api/auth/refresh" });
    }
}
