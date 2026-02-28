using DTO;
using Entities;

namespace Services
{
    public interface ISiteTypeService
    {
        Task<IEnumerable<SiteTypeDTO>?> GetAllAsync();
        Task<SiteTypeDTO?> GetByIdAsync(int id);
        Task<SiteTypeDTO?> UpdateByMngAsync(int id, SiteTypeDTO dto);
    }
}