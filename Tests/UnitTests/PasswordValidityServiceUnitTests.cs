using Services;

namespace Tests.UnitTests
{
    public class PasswordValidityServiceUnitTests
    {
        [Fact]
        public void PasswordStrength_WithNullPassword_ReturnsNull()
        {
            var service = new PasswordValidityService();

            var result = service.PasswordStrength(null!);

            Assert.Null(result);
        }

        [Fact]
        public void PasswordStrength_WithStrongPassword_ReturnsDtoWithScore()
        {
            var service = new PasswordValidityService();

            var result = service.PasswordStrength("A-Strong_Password-2026!");

            Assert.NotNull(result);
            Assert.Equal("A-Strong_Password-2026!", result.Password);
            Assert.InRange(result.Strength, 0, 4);
        }
    }
}
