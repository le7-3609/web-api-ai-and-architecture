using System.Security.Claims;

namespace Services;

public interface IJwtService
{
    /// <summary>Creates a signed, short-lived access token containing userId, email, and role claims.</summary>
    string GenerateAccessToken(long userId, string email, string role);

    /// <summary>Generates a cryptographically random opaque refresh token (raw value – hash before persisting).</summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates the token signature, issuer, audience, and lifetime.
    /// Returns the ClaimsPrincipal on success; null on any failure.
    /// </summary>
    Task<ClaimsPrincipal?> ValidateAccessTokenAsync(string token);
}
