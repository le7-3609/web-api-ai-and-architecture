using Entities;

namespace Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<MainCategory>> GetAsync();
        Task<MainCategory> GetByIdAsync(int id);
    }
}