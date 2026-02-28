using Entities;

namespace Repositories
{
    public interface IRatingRepository
    {
        Task<Rating> AddRatingAsync(Rating rating);
    }
}