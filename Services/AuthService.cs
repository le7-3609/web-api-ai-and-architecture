using AutoMapper;
using DTO;
using Entities;
using Microsoft.Extensions.Options;
using Repositories;
using System.Security.Cryptography;
using System.Text;

namespace Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;
    private readonly IPasswordValidityService _passwordService;
    private readonly IMapper _mapper;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUserRepository userRepository,
        IUserService userService,
        IJwtService jwtService,
        IPasswordValidityService passwordService,
        IMapper mapper,
        IOptions<JwtSettings> jwtOptions)
    {
        _userRepository = userRepository;
        _userService = userService;
        _jwtService = jwtService;
        _passwordService = passwordService;
        _mapper = mapper;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<(AuthResultDTO? Result, string? Error)> LoginAsync(LoginDTO dto)
    {
        var user = await _userRepository.LoginAsync(dto.Email, dto.Password);
        if (user == null)
            return (null, "Invalid email or password.");

        user.LastLogin = DateTime.UtcNow;

        var result = await IssueTokensAsync(user);
        return (result, null);
    }

    public async Task<(AuthResultDTO? Result, string? Error)> RegisterAsync(RegisterDTO dto)
    {
        var passwordCheck = _passwordService.PasswordStrength(dto.Password);
        if (passwordCheck == null || passwordCheck.Strength < 2)
            return (null, "Password is too weak.");

        var existing = await _userRepository.GetByEmailAsync(dto.Email, -1);
        if (existing != null)
            return (null, "Email is already in use.");

        var user = _mapper.Map<User>(dto);
        user.Role = "User";
        var created = await _userRepository.RegisterAsync(user);

        var result = await IssueTokensAsync(created);
        return (result, null);
    }

    public async Task<AuthResultDTO?> RefreshAsync(string rawRefreshToken)
    {
        var tokenHash = HashToken(rawRefreshToken);
        var user = await _userRepository.GetByRefreshTokenAsync(tokenHash);

        if (user == null)
            return null;

        if (user.RefreshTokenExpiry < DateTime.UtcNow)
            return null;

        return await IssueTokensAsync(user);
    }

    public async Task LogoutAsync(long userId)
    {
        await _userRepository.SaveRefreshTokenAsync(userId, null, null);
    }

    public async Task<(AuthResultDTO? Result, string? Error)> SocialLoginAsync(SocialLoginDTO dto)
    {
        // IUserService.SocialLoginAsync handles external token verification and find-or-create.
        var profile = await _userService.SocialLoginAsync(dto);
        if (profile == null)
            return (null, "Authentication failed with external provider.");

        // Fetch the full entity so we have the Role claim for the JWT.
        var user = await _userRepository.GetByIdAsync((int)profile.UserId);
        if (user == null)
            return (null, "User not found after social login.");

        var result = await IssueTokensAsync(user);
        return (result, null);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<AuthResultDTO> IssueTokensAsync(User user)
    {
        var accessToken = _jwtService.GenerateAccessToken(user.UserId, user.Email, user.Role);
        var rawRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = HashToken(rawRefreshToken);
        var expiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);

        // Persist the hash – the raw token is only ever held by the client cookie.
        await _userRepository.SaveRefreshTokenAsync(user.UserId, refreshTokenHash, expiry);

        var userInfo = _mapper.Map<AuthResponseDTO>(user);
        return new AuthResultDTO(userInfo, accessToken, rawRefreshToken);
    }

    /// <summary>One-way SHA-256 hash used to store the refresh token safely in the DB.</summary>
    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }
}
