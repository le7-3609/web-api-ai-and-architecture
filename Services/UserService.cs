using AutoMapper;
using DTO;
using Entities;
using Repositories;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordValidityService _passwordService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public UserService(IUserRepository userRepository, IPasswordValidityService passwordService, IMapper mapper, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<IEnumerable<UserDTO>?> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();

            if (users == null)
                return null;

            return _mapper.Map<IEnumerable<UserDTO>>(users);
        }

        public async Task<UserProfileDTO?> RegisterAsync(RegisterDTO dto)
        {
            var passwordValidation = _passwordService.PasswordStrength(dto.Password);
            if (passwordValidation == null || passwordValidation.Strength < 2)
                return null;

            var existing = await _userRepository.GetByEmailAsync(dto.Email, -1);
            if (existing != null)
                return null;

            User user = _mapper.Map<User>(dto);
            var created = await _userRepository.RegisterAsync(user);

            return _mapper.Map<UserProfileDTO>(created);
        }

        public async Task<UserProfileDTO?> LoginAsync(LoginDTO dto)
        {
            var user = await _userRepository.LoginAsync(dto.Email, dto.Password);
            return user == null ? null : _mapper.Map<UserProfileDTO>(user);
        }

        public async Task<UserProfileDTO?> UpdateAsync(int id, UpdateUserDTO dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return null;

            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
            {
                var existing = await _userRepository.GetByEmailAsync(dto.Email, id);
                if (existing != null)
                    return null;
                user.Email = dto.Email;
            }

            if (!string.IsNullOrEmpty(dto.Password))
            {
                var passwordValidation = _passwordService.PasswordStrength(dto.Password);
                if (passwordValidation == null || passwordValidation.Strength < 2)
                    return null;
                user.Password = dto.Password;
            }

            if (!string.IsNullOrEmpty(dto.FirstName))
                user.FirstName = dto.FirstName;
            if (!string.IsNullOrEmpty(dto.LastName))
                user.LastName = dto.LastName;
            if (!string.IsNullOrEmpty(dto.Phone))
                user.Phone = dto.Phone;

            var updated = await _userRepository.UpdateAsync(user);
            return _mapper.Map<UserProfileDTO>(updated);
        }

        public async Task<UserProfileDTO?> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);

            if (user == null)
                return null;

            return _mapper.Map<UserProfileDTO>(user);
        }

        public async Task<IEnumerable<OrderSummaryDTO>?> GetAllOrdersAsync(int userId)
        {
            var userOrders = await _userRepository.GetAllOrdersAsync(userId);

            if (userOrders == null)
                return null;

            return _mapper.Map<IEnumerable<OrderSummaryDTO>>(userOrders);
        }

        public async Task<UserProfileDTO?> SocialLoginAsync(SocialLoginDTO dto)
        {
            var externalUser = await VerifyExternalToken(dto.Token, dto.Provider);
            if (externalUser == null) return null;

            var existingUser = await _userRepository.GetByProviderIdAsync(dto.Provider, externalUser.ProviderId);

            if (existingUser != null)
            {
                existingUser.LastLogin = DateTime.UtcNow;
                await _userRepository.UpdateAsync(existingUser);
                return _mapper.Map<UserProfileDTO>(existingUser);
            }

            var newUser = new User
            {
                Email = externalUser.Email,
                FirstName = externalUser.FirstName,
                LastName = externalUser.LastName,
                Provider = dto.Provider,
                ProviderId = externalUser.ProviderId,
                Password = null, 
                CreatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.RegisterAsync(newUser);

            return _mapper.Map<UserProfileDTO>(createdUser);
        }

        private async Task<ExternalUserInfo?> VerifyExternalToken(string token, string provider)
        {
            if (provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
            {
                var googleClientId = _configuration["Authentication:Google:ClientId"];

                if (string.IsNullOrWhiteSpace(googleClientId) || 
                    googleClientId.StartsWith("YOUR_") || 
                    googleClientId.Equals("Secret_Stored_Locally", StringComparison.OrdinalIgnoreCase) ||
                    !googleClientId.EndsWith(".apps.googleusercontent.com", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Google ClientId is not configured. Set Authentication:Google:ClientId in appsettings or user secrets.");

                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new[] { googleClientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);

                return new ExternalUserInfo
                {
                    ProviderId = payload.Subject,
                    Email = payload.Email,
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                    Provider = "Google"
                };
            }

            if (provider.Equals("Microsoft", StringComparison.OrdinalIgnoreCase))
            {
                var handler = new JsonWebTokenHandler();
                var jwtToken = handler.ReadJsonWebToken(token);

                return new ExternalUserInfo
                {
                    ProviderId = jwtToken.GetClaim("oid")?.Value ?? jwtToken.Subject,
                    Email = jwtToken.GetClaim("preferred_username")?.Value ?? jwtToken.GetClaim("email")?.Value,
                    FirstName = jwtToken.GetClaim("name")?.Value.Split(' ')[0] ?? "",
                    LastName = jwtToken.GetClaim("name")?.Value.Split(' ').Last() ?? "",
                    Provider = "Microsoft"
                };
            }

            throw new NotSupportedException($"Provider {provider} is not supported.");
        }
    }
}
