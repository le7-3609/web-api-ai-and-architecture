using DTO;
using Moq;
using Services;

namespace Tests.UnitTests
{
    public class ChatBotServiceUnitTests
    {
        [Fact]
        public async Task SendMessageAsync_WithNullRequest_ThrowsInvalidOperationException()
        {
            var geminiChatService = new Mock<IGeminiChatService>();
            var service = new ChatBotService(geminiChatService.Object);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendMessageAsync(null!));

            Assert.Equal("Request body is required", exception.Message);
            geminiChatService.Verify(x => x.SendMessageAsync(It.IsAny<ChatRequestDTO>()), Times.Never);
        }

        [Fact]
        public async Task SendMessageAsync_WithValidRequest_DelegatesToGeminiService()
        {
            var geminiChatService = new Mock<IGeminiChatService>();
            var request = new ChatRequestDTO(new List<ChatMessageDTO>(), "hello");
            geminiChatService.Setup(x => x.SendMessageAsync(request)).ReturnsAsync("ok");

            var service = new ChatBotService(geminiChatService.Object);

            var result = await service.SendMessageAsync(request);

            Assert.Equal("ok", result);
            geminiChatService.Verify(x => x.SendMessageAsync(request), Times.Once);
        }
    }
}
