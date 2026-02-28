using DTO;
using Entities;

namespace Services
{
    public interface IGeminiService
    {
        Task<GeminiPromptDTO> AddGeminiForUserProductAsync(int? categoryId, string userRequest);
        Task<GeminiPromptDTO> AddGeminiForUserCategoryAsync(int subCategoryId, string userRequest);
        Task<GeminiPromptDTO> AddGeminiForBasicSiteAsync(string userRequest);
        Task<GeminiPromptDTO?> GetByIdPromptAsync(long promptId);
        Task<GeminiPromptDTO> UpdateGeminiForUserProductAsync(long promptId, string userRequest);
        Task<GeminiPromptDTO> UpdateGeminiForUserCategoryAsync(long promptId, string userRequest);
        Task<GeminiPromptDTO> UpdateGeminiForBasicSiteAsync(long promptId, string userRequest);
        Task DeletePromptAsync(long promptId);

    }
}