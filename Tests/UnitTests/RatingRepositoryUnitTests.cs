using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using Xunit;

namespace Tests.UnitTests
{
    public class RatingRepositoryUnitTests
    {
        private static MyShopContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<MyShopContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new MyShopContext(options);
        }

        #region Happy Paths

        [Fact]
        public async Task AddRatingAsync_WithValidRating_ReturnsRating()
        {
            var ctx = CreateInMemoryContext(nameof(AddRatingAsync_WithValidRating_ReturnsRating));
            var repo = new RatingRepository(ctx);

            var rating = new Rating { RatingId = 1, Method = "GET", Path = "/api/products", Host = "localhost" };
            var result = await repo.AddRatingAsync(rating);

            Assert.True(result.RatingId > 0);
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task AddRatingAsync_WithNullData_StillSaves()
        {
            var ctx = CreateInMemoryContext(nameof(AddRatingAsync_WithNullData_StillSaves));
            var repo = new RatingRepository(ctx);

            var rating = new Rating { Method = "POST", Path = "/api/orders" };
            var result = await repo.AddRatingAsync(rating);

            Assert.True(result.RatingId > 0);
        }

        #endregion
    }
}
