using DTO;

namespace Services;

public interface IAuthService
{
    /// <summary>Validates credentials and returns tokens. Returns (null, error) on failure.</summary>
    Task<(AuthResultDTO? Result, string? Error)> LoginAsync(LoginDTO dto);

    /// <summary>Registers a new user and immediately issues tokens. Returns (null, error) on failure.</summary>
    Task<(AuthResultDTO? Result, string? Error)> RegisterAsync(RegisterDTO dto);

    /// <summary>Validates an external OAuth token, finds or creates the user, and issues tokens.</summary>
    Task<(AuthResultDTO? Result, string? Error)> SocialLoginAsync(SocialLoginDTO dto);

    /// <summary>
    /// Rotates tokens using a valid refresh token.
    /// The refresh token value must be the raw (un-hashed) token from the cookie.
    /// Returns null when the token is invalid or expired.
    /// </summary>
    Task<AuthResultDTO?> RefreshAsync(string rawRefreshToken);

    /// <summary>Clears the persisted refresh token, invalidating any active sessions for the user.</summary>
    Task LogoutAsync(long userId);
}
