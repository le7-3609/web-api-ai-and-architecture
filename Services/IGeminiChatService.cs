using DTO;

namespace Services
{
    public interface IGeminiChatService
    {
        Task<string> SendMessageAsync(ChatRequestDTO request);
    }
}
