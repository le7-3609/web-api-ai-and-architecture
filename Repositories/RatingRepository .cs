using Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories
{
    public class RatingRepository : IRatingRepository
    {
        MyShopContext _context;

        public RatingRepository(MyShopContext context) 
        {
            _context = context;
        }

        async public Task<Rating> AddRatingAsync(Rating rating)
        {
            await _context.Ratings.AddAsync(rating);
            await _context.SaveChangesAsync();
            return rating;
        }
    }
}
