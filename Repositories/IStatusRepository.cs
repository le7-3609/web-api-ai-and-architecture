using Entities;

namespace Repositories
{
    public interface IStatusRepository
    {
        Task<IEnumerable<Status>> GetAllStatusesAsync();
        Task<Status?> GetStatusByIdAsync(int id);
    }
}
