using DTO;

namespace Services
{
    public interface IStatusService
    {
        Task<IEnumerable<StatusesDTO>> GetAllStatusesAsync();
        Task<StatusesDTO?> GetStatusByIdAsync(int id);
    }
}
