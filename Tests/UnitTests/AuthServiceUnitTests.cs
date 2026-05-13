using AutoMapper;
using BCrypt.Net;
using DTO;
using Entities;
using Microsoft.Extensions.Options;
using Moq;
using Repositories;
using Services;
using Xunit;

namespace Tests.UnitTests
{
    public class AuthServiceUnitTests
    {
        // ────────────────────────────────────────────────────────────────────
        // Helpers
        // ────────────────────────────────────────────────────────────────────

        private static (
            Mock<IUserRepository> repo,
            Mock<IUserService> userService,
            Mock<IJwtService> jwt,
            Mock<IPasswordValidityService> passwordService,
            Mock<IMapper> mapper,
            AuthService sut) BuildSut()
        {
            var repo = new Mock<IUserRepository>();
            var userService = new Mock<IUserService>();
            var jwt = new Mock<IJwtService>();
            var passwordService = new Mock<IPasswordValidityService>();
            var mapper = new Mock<IMapper>();

            var jwtSettings = Options.Create(new JwtSettings
            {
                SecretKey = "test-secret",
                Issuer = "test",
                Audience = "test",
                AccessTokenExpiryMinutes = 15,
                RefreshTokenExpiryDays = 7
            });

            var sut = new AuthService(repo.Object, userService.Object, jwt.Object,
                                      passwordService.Object, mapper.Object, jwtSettings);

            return (repo, userService, jwt, passwordService, mapper, sut);
        }

        private static User MakeUser(string email = "user@test.com", string plainPassword = "Secret-1!") =>
            new User
            {
                UserId = 1,
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                FirstName = "Test",
                LastName = "User",
                Role = "User"
            };

        // ────────────────────────────────────────────────────────────────────
        // Happy Paths
        // ────────────────────────────────────────────────────────────────────

        #region Happy Paths

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsAuthResult()
        {
            var (repo, _, jwt, _, mapper, sut) = BuildSut();
            var user = MakeUser();
            var dto = new LoginDTO(user.Email, "Secret-1!");
            var authResponse = new AuthResponseDTO(user.UserId, user.Email, user.FirstName, user.LastName, user.Role);

            repo.Setup(r => r.GetByEmailForAuthAsync(user.Email)).ReturnsAsync(user);
            jwt.Setup(j => j.GenerateAccessToken(user.UserId, user.Email, user.Role)).Returns("access-token");
            jwt.Setup(j => j.GenerateRefreshToken()).Returns("raw-refresh-token");
            repo.Setup(r => r.SaveRefreshTokenAsync(user.UserId, It.IsAny<string>(), It.IsAny<DateTime?>()))
                .Returns(Task.CompletedTask);
            mapper.Setup(m => m.Map<AuthResponseDTO>(user)).Returns(authResponse);

            var (result, error) = await sut.LoginAsync(dto);

            Assert.Null(error);
            Assert.NotNull(result);
            Assert.Equal("access-token", result!.AccessToken);
            Assert.Equal("raw-refresh-token", result.RefreshToken);
        }

        [Fact]
        public async Task RegisterAsync_ValidData_HashesPasswordAndReturnsAuthResult()
        {
            var (repo, _, jwt, passwordService, mapper, sut) = BuildSut();
            var dto = new RegisterDTO("new@test.com", "New", "User", "050", "Strong-123!", "Local");
            User? capturedUser = null;
            var savedUser = new User { UserId = 5, Email = "new@test.com", Role = "User" };
            var authResponse = new AuthResponseDTO(5, "new@test.com", "New", "User", "User");

            passwordService.Setup(p => p.PasswordStrength("Strong-123!"))
                .Returns(new PasswordStrengthDTO { Strength = 3 });
            repo.Setup(r => r.GetByEmailAsync("new@test.com", -1)).ReturnsAsync((User?)null);
            mapper.Setup(m => m.Map<User>(dto)).Returns(new User { Email = "new@test.com" });
            repo.Setup(r => r.RegisterAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync(savedUser);
            jwt.Setup(j => j.GenerateAccessToken(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("access-token");
            jwt.Setup(j => j.GenerateRefreshToken()).Returns("raw-refresh-token");
            repo.Setup(r => r.SaveRefreshTokenAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                .Returns(Task.CompletedTask);
            mapper.Setup(m => m.Map<AuthResponseDTO>(savedUser)).Returns(authResponse);

            var (result, error) = await sut.RegisterAsync(dto);

            Assert.Null(error);
            Assert.NotNull(result);
            // Password must be hashed — plain text must never reach the repository
            Assert.NotNull(capturedUser);
            Assert.NotEqual("Strong-123!", capturedUser!.Password);
            Assert.True(BCrypt.Net.BCrypt.Verify("Strong-123!", capturedUser.Password));
        }

        #endregion

        // ────────────────────────────────────────────────────────────────────
        // Unhappy Paths
        // ────────────────────────────────────────────────────────────────────

        #region Unhappy Paths

        [Fact]
        public async Task LoginAsync_UserNotFound_ReturnsError()
        {
            var (repo, _, _, _, _, sut) = BuildSut();
            repo.Setup(r => r.GetByEmailForAuthAsync("ghost@test.com")).ReturnsAsync((User?)null);

            var (result, error) = await sut.LoginAsync(new LoginDTO("ghost@test.com", "any"));

            Assert.Null(result);
            Assert.Equal("Invalid email or password.", error);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ReturnsError()
        {
            var (repo, _, _, _, _, sut) = BuildSut();
            var user = MakeUser("u@test.com", "correct-pass");
            repo.Setup(r => r.GetByEmailForAuthAsync("u@test.com")).ReturnsAsync(user);

            var (result, error) = await sut.LoginAsync(new LoginDTO("u@test.com", "wrong-pass"));

            Assert.Null(result);
            Assert.Equal("Invalid email or password.", error);
        }

        [Fact]
        public async Task RegisterAsync_WeakPassword_ReturnsError()
        {
            var (_, _, _, passwordService, _, sut) = BuildSut();
            passwordService.Setup(p => p.PasswordStrength("weak")).Returns(new PasswordStrengthDTO { Strength = 1 });

            var (result, error) = await sut.RegisterAsync(
                new RegisterDTO("a@a.com", "A", "B", "050", "weak", "Local"));

            Assert.Null(result);
            Assert.Equal("Password is too weak.", error);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ReturnsError()
        {
            var (repo, _, _, passwordService, _, sut) = BuildSut();
            passwordService.Setup(p => p.PasswordStrength("Strong-123!"))
                .Returns(new PasswordStrengthDTO { Strength = 3 });
            repo.Setup(r => r.GetByEmailAsync("taken@test.com", -1))
                .ReturnsAsync(new User { Email = "taken@test.com" });

            var (result, error) = await sut.RegisterAsync(
                new RegisterDTO("taken@test.com", "A", "B", "050", "Strong-123!", "Local"));

            Assert.Null(result);
            Assert.Equal("Email is already in use.", error);
            repo.Verify(r => r.RegisterAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_PasswordHashIsNeverPlainText()
        {
            var (repo, _, jwt, passwordService, mapper, sut) = BuildSut();
            const string plainPassword = "Super-Secure-99!";
            User? capturedUser = null;

            passwordService.Setup(p => p.PasswordStrength(plainPassword))
                .Returns(new PasswordStrengthDTO { Strength = 4 });
            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), -1)).ReturnsAsync((User?)null);
            mapper.Setup(m => m.Map<User>(It.IsAny<RegisterDTO>()))
                .Returns(new User { Email = "x@x.com" });
            repo.Setup(r => r.RegisterAsync(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u)
                .ReturnsAsync(new User { UserId = 1, Email = "x@x.com", Role = "User" });
            jwt.Setup(j => j.GenerateAccessToken(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("tok");
            jwt.Setup(j => j.GenerateRefreshToken()).Returns("ref");
            repo.Setup(r => r.SaveRefreshTokenAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                .Returns(Task.CompletedTask);
            mapper.Setup(m => m.Map<AuthResponseDTO>(It.IsAny<User>()))
                .Returns(new AuthResponseDTO(1, "x@x.com", null, null, "User"));

            await sut.RegisterAsync(new RegisterDTO("x@x.com", "X", "Y", "050", plainPassword, "Local"));

            Assert.NotNull(capturedUser);
            Assert.NotEqual(plainPassword, capturedUser!.Password);
            Assert.StartsWith("$2", capturedUser.Password); // BCrypt hash prefix
        }

        #endregion
    }
}
