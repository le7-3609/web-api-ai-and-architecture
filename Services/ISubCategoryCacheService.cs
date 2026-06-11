using DTO;

namespace Services
{
    public interface ISubCategoryCacheService
    {
        Task<(IEnumerable<SubCategoryDTO>? Items, int TotalCount)> GetSubCategoryListAsync(string cacheKey);
        Task SetSubCategoryListAsync(string cacheKey, IEnumerable<SubCategoryDTO> items, int totalCount);
        Task<string> BuildListCacheKeyAsync(int position, int skip, string? desc, int?[] mainCategoryIds);
        Task InvalidateSubCategoryListsAsync();
    }
}