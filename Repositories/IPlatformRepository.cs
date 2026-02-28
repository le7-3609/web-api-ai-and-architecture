using Entities;

namespace Repositories
{
    public interface IPlatformRepository
    {
        Task<Platform> AddPlatformAsync(Platform platform);
        Task<bool> DeletePlatformAsync(int id);
        Task ReassignPlatformReferencesAsync(int platformId, int defaultPlatformId);
        Task<IEnumerable<Platform>> GetPlatformsAsync();
        Task<bool> UpdatePlatformAsync(int id, Platform platform);
        Task<Platform?> GetPlatformByIdAsync(int id);
        Task<Platform?> GetPlatformByNameAsync(string name);
    }
}