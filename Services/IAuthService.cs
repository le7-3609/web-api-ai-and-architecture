using DTO;

namespace Services;

public interface IAuthService
{
    Task<(AuthResultDTO? Result, string? Error)> LoginAsync(LoginDTO dto);
    Task<(AuthResultDTO? Result, string? Error)> RegisterAsync(RegisterDTO dto);
    Task<(AuthResultDTO? Result, string? Error)> SocialLoginAsync(SocialLoginDTO dto);
    Task<AuthResultDTO?> RefreshAsync(string rawRefreshToken);
    Task LogoutAsync(long userId);
}
