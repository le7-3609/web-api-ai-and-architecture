using DTO;

namespace Services
{
    public interface IChatBotService
    {
        Task<string> SendMessageAsync(ChatRequestDTO request);
    }
}
