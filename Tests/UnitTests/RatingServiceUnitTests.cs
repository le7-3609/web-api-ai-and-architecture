using Entities;
using Moq;
using Repositories;
using Services;

namespace Tests.UnitTests
{
    public class RatingServiceUnitTests
    {
        [Fact]
        public async Task AddRatingAsync_WithValidRating_ReturnsSavedEntity()
        {
            var repository = new Mock<IRatingRepository>();
            var rating = new Rating { RatingId = 1, Host = "localhost", Method = "GET", Path = "/api/test" };
            repository.Setup(r => r.AddRatingAsync(rating)).ReturnsAsync(rating);

            var service = new RatingService(repository.Object);

            var result = await service.AddRatingAsync(rating);

            Assert.Equal(1, result.RatingId);
            repository.Verify(r => r.AddRatingAsync(rating), Times.Once);
        }

        [Fact]
        public async Task AddRatingAsync_WhenRepositoryReturnsNull_ReturnsNull()
        {
            var repository = new Mock<IRatingRepository>();
            repository.Setup(r => r.AddRatingAsync(It.IsAny<Rating>())).ReturnsAsync((Rating)null!);

            var service = new RatingService(repository.Object);

            var result = await service.AddRatingAsync(new Rating());

            Assert.Null(result);
        }
    }
}
