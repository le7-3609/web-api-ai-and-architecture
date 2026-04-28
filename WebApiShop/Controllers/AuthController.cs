using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Security.Claims;

namespace WebApiShop.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuthCookieService _authCookieService;

    public AuthController(IAuthService authService, IAuthCookieService authCookieService)
    {
        _authService = authService;
        _authCookieService = authCookieService;
    }

    // POST api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDTO>> LoginAsync([FromBody] LoginDTO dto)
    {
        var (result, error) = await _authService.LoginAsync(dto);
        if (result == null) return Unauthorized(error);

        _authCookieService.SetAuthCookies(result.AccessToken, result.RefreshToken);
        return Ok(result.UserInfo);
    }

    // POST api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDTO>> RegisterAsync([FromBody] RegisterDTO dto)
    {
        var (result, error) = await _authService.RegisterAsync(dto);
        if (result == null) return BadRequest(error);

        _authCookieService.SetAuthCookies(result.AccessToken, result.RefreshToken);
        return CreatedAtAction(nameof(LoginAsync), result.UserInfo);
    }

    // POST api/auth/refresh
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDTO>> RefreshAsync()
    {
        var rawRefreshToken = _authCookieService.GetRefreshToken();
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
            return Unauthorized("Refresh token not found.");

        var result = await _authService.RefreshAsync(rawRefreshToken);
        if (result == null)
            return Unauthorized("Invalid or expired refresh token.");

        _authCookieService.SetAuthCookies(result.AccessToken, result.RefreshToken);
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

        _authCookieService.DeleteAuthCookies();
        return NoContent();
    }

    // POST api/auth/social-login
    [HttpPost("social-login")]
    public async Task<ActionResult<AuthResponseDTO>> SocialLoginAsync([FromBody] SocialLoginDTO dto)
    {
        var (result, error) = await _authService.SocialLoginAsync(dto);
        if (result == null) return Unauthorized(error);

        _authCookieService.SetAuthCookies(result.AccessToken, result.RefreshToken);
        return Ok(result.UserInfo);
    }
}
