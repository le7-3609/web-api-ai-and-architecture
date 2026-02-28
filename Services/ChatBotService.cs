using DTO;

namespace Services
{
    public class ChatBotService : IChatBotService
    {
        private readonly IGeminiChatService _geminiChatService;

        public ChatBotService(IGeminiChatService geminiChatService)
        {
            _geminiChatService = geminiChatService;
        }

        public async Task<string> SendMessageAsync(ChatRequestDTO request)
        {
            if (request == null)
            {
                throw new InvalidOperationException("Request body is required");
            }

            if (string.IsNullOrWhiteSpace(request.NewMessage))
            {
                throw new InvalidOperationException("NewMessage is required");
            }

            return await _geminiChatService.SendMessageAsync(request);
        }
    }
}
