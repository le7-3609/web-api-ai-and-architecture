using DTO;

namespace Services
{
    public interface ISubCategoryService
    {
        Task<SubCategoryDTO> AddSubCategoryAsync(AddSubCategoryDTO dto);
        Task<bool> DeleteSubCategoryAsync(int id);
        Task<(IEnumerable<SubCategoryDTO>, int TotalCount)> GetSubCategoryAsync(int position, int skip, string? desc, int?[] mainCategoryIds);
        Task<SubCategoryDTO?> GetSubCategoryByIdAsync(int id);
        Task UpdateSubCategoryAsync(int id, SubCategoryDTO dto);
    }
}