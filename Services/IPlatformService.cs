using DTO;

namespace Services
{
    public interface IPlatformService
    {
        Task<PlatformsDTO?> AddPlatformAsync(string platformName);
        Task<bool> DeletePlatformAsync(int id);
        Task<IEnumerable<PlatformsDTO>> GetPlatformsAsync();
        Task<bool> UpdatePlatformAsync(int id, PlatformsDTO dto);
        Task<PlatformsDTO?> GetPlatformByIdAsync(int id);
    }
}