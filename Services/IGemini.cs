
namespace Services
{
    public interface IGemini
    {
        Task<string?> RunGeminiForUserProduct(string userRequest, string category);
        Task<string?> RunGeminiForFillCategory(string userRequest, string mainCategory);
        Task<string?> RunGeminiForFillBasicSite(string userRequest);
    }
}