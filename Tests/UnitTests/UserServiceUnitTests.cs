using AutoMapper;
using BCrypt.Net;
using DTO;
using Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using Repositories;
using Services;
using System.Text;
using System.Text.Json;

namespace Tests.UnitTests
{
    public class UserServiceUnitTests
    {
        [Fact]
        public async Task SocialLoginAsync_UnsupportedProvider_ThrowsNotSupportedException()
        {
            var repository = new Mock<IUserRepository>();
            var passwordService = new Mock<IPasswordValidityService>();
            var mapper = new Mock<IMapper>();
            var configuration = new Mock<IConfiguration>();

            var service = new UserService(repository.Object, passwordService.Object, mapper.Object, configuration.Object);

            await Assert.ThrowsAsync<NotSupportedException>(() =>
                service.SocialLoginAsync(new SocialLoginDTO("fake-token", "GitHub")));
        }

        [Fact]
        public async Task SocialLoginAsync_MicrosoftToken_ExistingUser_ReturnsProfileAndUpdatesUser()
        {
            var repository = new Mock<IUserRepository>();
            var passwordService = new Mock<IPasswordValidityService>();
            var mapper = new Mock<IMapper>();
            var configuration = new Mock<IConfiguration>();

            var token = BuildMicrosoftJwt("oid-123", "microsoft.user@contoso.com", "Jane Doe");
            var existingUser = new User
            {
                UserId = 11,
                Provider = "Microsoft",
                ProviderId = "oid-123",
                Email = "microsoft.user@contoso.com",
                FirstName = "Jane",
                LastName = "Doe",
                Phone = "0500000000",
                LastLogin = null
            };
            var expectedProfile = new UserProfileDTO(11, "microsoft.user@contoso.com", "Jane", "Doe", "0500000000");

            repository.Setup(r => r.GetByProviderIdAsync("Microsoft", "oid-123"))
                .ReturnsAsync(existingUser);
            repository.Setup(r => r.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => u);
            mapper.Setup(m => m.Map<UserProfileDTO>(existingUser))
                .Returns(expectedProfile);

            var service = new UserService(repository.Object, passwordService.Object, mapper.Object, configuration.Object);
            var result = await service.SocialLoginAsync(new SocialLoginDTO(token, "Microsoft"));

            Assert.NotNull(result);
            Assert.Equal(11, result!.UserId);
            repository.Verify(r => r.UpdateAsync(It.Is<User>(u =>
                u.ProviderId == "oid-123" &&
                u.LastLogin.HasValue)), Times.Once);
            repository.Verify(r => r.RegisterAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WithWeakPassword_ReturnsNull()
        {
            var repository = new Mock<IUserRepository>();
            var passwordService = new Mock<IPasswordValidityService>();
            var mapper = new Mock<IMapper>();
            var configuration = new Mock<IConfiguration>();

            passwordService.Setup(p => p.PasswordStrength("weak")).Returns(new PasswordStrengthDTO { Strength = 1 });
            var service = new UserService(repository.Object, passwordService.Object, mapper.Object, configuration.Object);

            var result = await service.RegisterAsync(new RegisterDTO("a@a.com", "A", "B", "050", "weak", "Local"));

            Assert.Null(result.User);
            repository.Verify(r => r.RegisterAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_ReturnsUserProfile()
        {
            var repository = new Mock<IUserRepository>();
            var passwordService = new Mock<IPasswordValidityService>();
            var mapper = new Mock<IMapper>();
            var configuration = new Mock<IConfiguration>();

            var dto = new RegisterDTO("x@x.com", "X", "Y", "050", "Strong-123!", "Local");
            var mappedUser = new User { UserId = 10, Email = "x@x.com", FirstName = "X", LastName = "Y", Phone = "050" };
            var profile = new UserProfileDTO(10, "x@x.com", "X", "Y", "050");
            User? capturedUser = null;

            passwordService.Setup(p => p.PasswordStrength("Strong-123!")).Returns(new PasswordStrengthDTO { Strength = 3 });
            repository.Setup(r => r.GetByEmailAsync("x@x.com", -1)).ReturnsAsync((User)null!);
            mapper.Setup(m => m.Map<User>(dto)).Returns(mappedUser);
            repository.Setup(r => r.RegisterAsync(It.IsAny<User>()))
                      .Callback<User>(u => capturedUser = u)
                      .ReturnsAsync(mappedUser);
            mapper.Setup(m => m.Map<UserProfileDTO>(mappedUser)).Returns(profile);

            var service = new UserService(repository.Object, passwordService.Object, mapper.Object, configuration.Object);

            var result = await service.RegisterAsync(dto);

            Assert.NotNull(result);
            Assert.NotNull(result.User);
            Assert.Equal(10, result.User!.UserId);
            // Password must be stored as a BCrypt hash — never plain text
            Assert.NotNull(capturedUser);
            Assert.NotEqual("Strong-123!", capturedUser!.Password);
            Assert.True(BCrypt.Net.BCrypt.Verify("Strong-123!", capturedUser.Password));
            repository.Verify(r => r.RegisterAsync(It.IsAny<User>()), Times.Once);
        }

        private static string BuildMicrosoftJwt(string oid, string preferredUsername, string name)
        {
            var header = new { alg = "none", typ = "JWT" };
            var payload = new
            {
                oid,
                preferred_username = preferredUsername,
                name
            };

            return $"{Base64UrlEncodeJson(header)}.{Base64UrlEncodeJson(payload)}.signature";
        }

        private static string Base64UrlEncodeJson<T>(T value)
        {
            var json = JsonSerializer.Serialize(value);
            var bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
