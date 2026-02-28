using DTO;
using Entities;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class RatingService : IRatingService
    {
        private readonly IRatingRepository _ratingRepository;

        public RatingService(IRatingRepository ratingRepository) 
        {
            _ratingRepository = ratingRepository;
        }
        async public Task<Rating> AddRatingAsync(Rating rating)
        {
            return await _ratingRepository.AddRatingAsync(rating);
        }

    }
}
