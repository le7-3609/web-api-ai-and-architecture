using Entities;
using Repositories;
using Xunit;

namespace Tests.IntegrationTests
{
    [Collection("Database collection")]
    public class OrderRepositoryIntegrationTests
    {
        private readonly DatabaseFixture _fixture;
        private readonly MyShopContext _context;
        private readonly OrderRepository _repository;

        public OrderRepositoryIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Context;
            _repository = new OrderRepository(_context);
            _fixture.ClearDatabase();
        }

        #region Happy Paths

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsOrder()
        {
            var order = new Order { OrderId = 1, OrderSum = 100 };
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(100, result.OrderSum);
        }

        [Fact]
        public async Task AddOrderAsync_WithValidOrder_ReturnsOrder()
        {
            var order = new Order { OrderSum = 250 };

            var result = await _repository.AddOrderAsync(order);

            Assert.True(result.OrderId > 0);
        }

        [Fact]
        public async Task AddReviewAsync_WithValidReview_ReturnsReview()
        {
            var order = new Order { OrderSum = 100 };
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            var review = new Review { OrderId = order.OrderId, Score = 5, Note = "Great!", ReviewImageUrl = "url" };
            var result = await _repository.AddReviewAsync(review);

            Assert.True(result.ReviewId > 0);
        }

        [Fact]
        public async Task GetOrderItemsAsync_WithExistingOrderItems_ReturnsItems()
        {
            var order = new Order { OrderSum = 100 };
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            await _context.MainCategories.AddAsync(new MainCategory { MainCategoryId = 1, MainCategoryName = "M", MainCategoryPrompt = "P" });
            await _context.SubCategories.AddAsync(new SubCategory { SubCategoryId = 1, MainCategoryId = 1, SubCategoryName = "S", SubCategoryPrompt = "P" });
            await _context.Products.AddAsync(new Product { ProductId = 1, SubCategoryId = 1, ProductName = "Prod", ProductPrompt = "P" });
            await _context.Platforms.AddAsync(new Platform { PlatformId = 1, PlatformName = "Plat" });
            await _context.SaveChangesAsync();

            var item = new OrderItem { OrderId = order.OrderId, ProductId = 1, PlatformId = 1 };
            await _context.OrderItems.AddAsync(item);
            await _context.SaveChangesAsync();

            var result = await _repository.GetOrderItemsAsync((int)order.OrderId);

            Assert.Single(result);
        }

        [Fact]
        public async Task GetReviewByOrderIdAsync_WithExistingReview_ReturnsReview()
        {
            var order = new Order { OrderSum = 100 };
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            var review = new Review { OrderId = order.OrderId, Score = 4, Note = "Good", ReviewImageUrl = "url" };
            await _context.Reviews.AddAsync(review);
            await _context.SaveChangesAsync();

            var result = await _repository.GetReviewByOrderIdAsync((int)order.OrderId);

            Assert.NotNull(result);
            Assert.Equal((short)4, result.Score);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithValidOrder_UpdatesOrder()
        {
            var order = new Order { OrderId = 1, OrderSum = 100, Status = 1 };
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            order.Status = 2;
            await _repository.UpdateStatusAsync(order);

            var updated = await _context.Orders.FindAsync(1L);
            Assert.NotNull(updated);
            Assert.Equal(2, updated.Status);
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task GetByIdAsync_NonExistentId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(9999);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetOrderItemsAsync_EmptyOrder_ReturnsEmpty()
        {
            var order = new Order { OrderSum = 100 };
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            var result = await _repository.GetOrderItemsAsync((int)order.OrderId);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetReviewByOrderIdAsync_NoReview_ReturnsNull()
        {
            var order = new Order { OrderSum = 100 };
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            var result = await _repository.GetReviewByOrderIdAsync((int)order.OrderId);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateReviewAsync_WithValidReview_UpdatesReview()
        {
            var order = new Order { OrderSum = 100 };
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            var review = new Review { OrderId = order.OrderId, Score = 3, Note = "Old", ReviewImageUrl = "url" };
            await _context.Reviews.AddAsync(review);
            await _context.SaveChangesAsync();

            review.Score = 5;
            review.Note = "Updated";
            var result = await _repository.UpdateReviewAsync(review);

            Assert.Equal((short)5, result.Score);
        }

        #endregion
    }
}