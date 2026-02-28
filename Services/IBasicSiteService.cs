using DTO;

namespace Services
{
    public interface IBasicSiteService
    {
        Task<BasicSiteDTO> AddBasicSiteAsync(AddBasicSiteDTO dto);
        Task<BasicSiteDTO?> GetBasicSiteByIdAsync(int id);
        Task<double> GetBasicSitePriceAsync(long basicSiteId);
        Task UpdateBasicSiteAsync(int id, UpdateBasicSiteDTO dto);
    }
}