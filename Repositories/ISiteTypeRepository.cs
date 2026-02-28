using Entities;

namespace Repositories
{
    public interface ISiteTypeRepository
    {
        Task<IEnumerable<SiteType>?> GetAllAsync();
        Task<SiteType?> GetByIdAsync(int id);
        Task<SiteType?> GetByNameAsync(string name);
        Task<SiteType> UpdateByMngAsync(SiteType siteType);
    }
}