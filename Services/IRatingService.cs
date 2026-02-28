using Entities;

namespace Services
{
    public interface IRatingService
    {
        Task<Rating> AddRatingAsync(Rating rating);
    }
}