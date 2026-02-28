using Entities;

namespace Repositories
{
    public interface ISubCategoryRepository
    {
        Task<SubCategory> AddSubCategoryAsync(SubCategory category);
        Task<bool> DeleteSubCategoryAsync(int id);
        Task<bool> HasProductsAsync(int subCategoryId);
        Task<SubCategory?> GetSubCategoryByIdAsync(int id);
        Task<SubCategory?> GetByNameAsync(string name);
        Task<bool> MainCategoryExistsAsync(int mainCategoryId);
        Task UpdateSubCategoryAsync(int id, SubCategory category);
        Task<(IEnumerable<SubCategory>, int TotalCount)> GetSubCategoryAsync(int position, int skip, string? desc, int?[] mainCategoryIds);
    }
}