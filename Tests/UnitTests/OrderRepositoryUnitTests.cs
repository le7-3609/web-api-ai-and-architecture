using Moq;
using Moq.EntityFrameworkCore;
using Repositories;
using Entities;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.UnitTests
{
    public class OrderRepositoryUnitTesting
    {
        #region Happy Paths

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsOrder()
        {
            // Arrange
            var orders = new List<Order> { new Order { OrderId = 1, OrderSum = 100 } };
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Orders).ReturnsDbSet(orders);
            var repo = new OrderRepository(mockContext.Object);

            // Act
            var result = await repo.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.OrderId);
        }

        [Fact]
        public async Task AddOrderAsync_ValidOrder_ReturnsAddedOrder()
        {
            // Arrange
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order>());
            var repo = new OrderRepository(mockContext.Object);
            var newOrder = new Order { UserId = 1, OrderSum = 250 };

            // Act
            var result = await repo.AddOrderAsync(newOrder);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(250, result.OrderSum);
            mockContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_ValidUpdate_SuccessfullyUpdates()
        {
            // Arrange
            var order = new Order { OrderId = 1, Status = 1 }; // Status 1 = Pending
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order> { order });
            var repo = new OrderRepository(mockContext.Object);

            // Act
            order.Status = 2; // Status 2 = Shipped
            await repo.UpdateStatusAsync(order);

            // Assert
            mockContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task AddReviewAsync_ValidReview_ReturnsAddedReview()
        {
            // Arrange
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Reviews).ReturnsDbSet(new List<Review>());
            var repo = new OrderRepository(mockContext.Object);
            var review = new Review { OrderId = 1, Note = "Great product!", Score = 5 };

            // Act
            var result = await repo.AddReviewAsync(review);

            // Assert
            Assert.Equal("Great product!", result.Note);
            mockContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task GetReviewByOrderIdAsync_ExistingOrder_ReturnsReview()
        {
            // Arrange
            var reviews = new List<Review> { new Review { OrderId = 1, Note = "Nice" } };
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Reviews).ReturnsDbSet(reviews);
            var repo = new OrderRepository(mockContext.Object);

            // Act
            var result = await repo.GetReviewByOrderIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.OrderId);
        }

        [Fact]
        public async Task GetOrderItemsAsync_ExistingOrder_ReturnsItems()
        {
            // Arrange
            var items = new List<OrderItem> {
                new OrderItem { OrderItemId = 1, OrderId = 1 },
                new OrderItem { OrderItemId = 2, OrderId = 1 },
                new OrderItem { OrderItemId = 3, OrderId = 2 } // Different order
            };
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.OrderItems).ReturnsDbSet(items);
            var repo = new OrderRepository(mockContext.Object);

            // Act
            var result = await repo.GetOrderItemsAsync(1);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, item => Assert.Equal(1, item.OrderId));
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order>());
            var repo = new OrderRepository(mockContext.Object);

            // Act
            var result = await repo.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetReviewByOrderIdAsync_NoReview_ReturnsNull()
        {
            // Arrange
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Reviews).ReturnsDbSet(new List<Review>());
            var repo = new OrderRepository(mockContext.Object);

            // Act
            var result = await repo.GetReviewByOrderIdAsync(5);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetOrderItemsAsync_EmptyOrder_ReturnsEmptyList()
        {
            // Arrange
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.OrderItems).ReturnsDbSet(new List<OrderItem>());
            var repo = new OrderRepository(mockContext.Object);

            // Act
            var result = await repo.GetOrderItemsAsync(1);

            // Assert
            Assert.Empty(result);
        }

        #endregion
    }
}