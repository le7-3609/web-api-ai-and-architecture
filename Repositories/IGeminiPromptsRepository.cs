using Entities;

namespace Repositories
{
    public interface IGeminiPromptsRepository
    {
        Task<GeminiPrompt> AddPromptAsync(GeminiPrompt prompt);
        Task<GeminiPrompt?> GetByIDPromptAsync(long id);
        Task UpdatePromptAsync(long id, GeminiPrompt prompt);
        Task DeletePromptAsync(long id);
    }
}