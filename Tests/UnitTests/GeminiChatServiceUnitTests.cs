using DTO;
using Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Services;

namespace Tests.UnitTests
{
    public class GeminiChatServiceUnitTests
    {
        [Fact]
        public async Task SendMessageAsync_WhenApiKeyMissing_ThrowsInvalidOperationException()
        {
            var configuration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var logger = new Mock<ILogger<GeminiChatService>>();
            var options = Options.Create(new GeminiSettings { ApiKey = "" });
            var service = new GeminiChatService(configuration.Object, logger.Object, options);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SendMessageAsync(new ChatRequestDTO(
                    new List<ChatMessageDTO>(),
                    "hello")));
        }
    }
}
