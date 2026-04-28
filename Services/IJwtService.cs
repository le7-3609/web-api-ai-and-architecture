using System.Security.Claims;

namespace Services;

public interface IJwtService
{
    string GenerateAccessToken(long userId, string email, string role);
    string GenerateRefreshToken();
    Task<ClaimsPrincipal?> ValidateAccessTokenAsync(string token);
}
