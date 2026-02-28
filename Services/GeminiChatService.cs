using DTO;
using Entities;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Services
{
    public class GeminiChatService : IGeminiChatService
    {
        private readonly ILogger<GeminiChatService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IOptions<GeminiSettings> _geminiSettings;

        public GeminiChatService(
            IConfiguration configuration,
            ILogger<GeminiChatService> logger,
            IOptions<GeminiSettings> geminiSettings)
        {
            _configuration = configuration;
            _logger = logger;
            _geminiSettings = geminiSettings;
        }

        public async Task<string> SendMessageAsync(ChatRequestDTO request)
        {
            string apiKey = GetRequiredApiKey();
            var contents = BuildContents(request);
            var client = new Client(apiKey: apiKey);

            try
            {
                return await GenerateResponseTextAsync(client, contents);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini chat request failed");
                throw new InvalidOperationException("Failed to process chat request");
            }
        }

        private string GetRequiredApiKey()
        {
            string? apiKey = ResolveApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Gemini API key is missing");
            }

            return apiKey;
        }

        private static List<Content> BuildContents(ChatRequestDTO request)
        {
            var contents = BuildHistoryContents(request);
            contents.Add(CreateUserContent(request.NewMessage));
            return contents;
        }

        private static List<Content> BuildHistoryContents(ChatRequestDTO request)
        {
            var contents = new List<Content>();

            if (request.History == null || request.History.Count == 0)
            {
                return contents;
            }

            int messagesToSkip = Math.Max(0, request.History.Count - 12);
            var limitedHistory = request.History.Skip(messagesToSkip);

            foreach (var message in limitedHistory)
            {
                if (string.IsNullOrWhiteSpace(message?.Text))
                {
                    continue;
                }

                contents.Add(new Content
                {
                    Role = NormalizeRole(message.Role),
                    Parts = new List<Part> { new Part { Text = message.Text } }
                });
            }

            return contents;
        }

        private static Content CreateUserContent(string? text)
        {
            return new Content
            {
                Role = "user",
                Parts = new List<Part> { new Part { Text = text } }
            };
        }

        private async Task<string> GenerateResponseTextAsync(Client client, List<Content> contents)
        {
            var response = await client.Models.GenerateContentAsync(
                model: "gemini-3-flash-preview",
                contents: contents,
                config: new GenerateContentConfig
                {
                    Temperature = 0.2f,
                    SystemInstruction = BuildSystemInstruction()
                });

            return GetRequiredText(response.Candidates?[0].Content?.Parts?[0].Text);
        }

        private static Content BuildSystemInstruction()
        {
            return new Content
            {
                Role = "system",
                Parts = new List<Part>
                {
                    new Part
                    {
                        Text =
                            "You are the Prompt Store assistant for website builder prompts. " +
                            "Answer only about this platform, products, categories, pricing flow, and prompt usage. " +
                            "If a question is outside this scope, politely refuse and ask the user to ask about Prompt Store. " +
                            "Keep answers concise, practical, and in the user's language. " +
                            "Do not use markdown."
                    }
                }
            };
        }

        private static string GetRequiredText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Gemini returned an empty response");
            }

            return text;
        }

        private static string NormalizeRole(string? role)
        {
            if (string.Equals(role, "model", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                return "model";
            }

            return "user";
        }

        private string? ResolveApiKey()
        {
            if (!string.IsNullOrWhiteSpace(_geminiSettings.Value.ApiKey))
            {
                return _geminiSettings.Value.ApiKey;
            }

            return _configuration["GEMINI_API_KEY"];
        }
    }
}
