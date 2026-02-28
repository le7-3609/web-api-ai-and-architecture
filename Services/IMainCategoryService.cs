using DTO;

namespace Services
{
    public interface IMainCategoryService
    {
        Task<MainCategoryDTO> AddMainCategoryAsync(AddMainCategoryDTO dto);
        Task<AddMainCategoryDTO> GetMainCategoryByIdAsync(int id);     
        Task<bool> DeleteMainCategoryAsync(int id);
        Task<IEnumerable<MainCategoryDTO>> GetMainCategoryAsync();
        Task<bool> UpdateMainCategoryAsync(int id, AddMainCategoryDTO dto);
    }
}