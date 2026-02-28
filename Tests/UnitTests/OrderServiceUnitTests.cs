using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DTO;
using Entities;
using Moq;
using Repositories;
using Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Tests.UnitTests
{
    public class OrderServiceUnitTests
    {
        private readonly Mock<IOrderRepository> _mockOrderRepo = new();
        private readonly Mock<IProductRepository> _mockProductRepo = new();
        private readonly Mock<IMapper> _mockMapper = new();
        private readonly Mock<ICartService> _mockCartService = new();
        private readonly Mock<ILogger<OrderService>> _mockLogger = new();
        private readonly Mock<IHostEnvironment> _mockHostEnvironment = new();
        private readonly Mock<IOrderPromptBuilder> _mockPromptBuilder = new();

        private OrderService CreateService() => new(
            _mockOrderRepo.Object, _mockMapper.Object, _mockCartService.Object,
            _mockLogger.Object, _mockProductRepo.Object, _mockHostEnvironment.Object,
            _mockPromptBuilder.Object);

        [Fact]
        public async Task AddOrderFromCartAsync_InvalidCartId_Throws()
        {
            var svc = CreateService();
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AddOrderFromCartAsync(0));
        }

        [Fact]
        public async Task AddOrderFromCartAsync_CartNotFound_Throws()
        {
            _mockCartService.Setup(c => c.GetCartByIdAsync(5)).ReturnsAsync((CartDTO?)null);
            var svc = CreateService();
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.AddOrderFromCartAsync(5));
        }

        [Fact]
        public async Task AddOrderFromCartAsync_MissingBasicSiteId_Throws()
        {
            _mockCartService.Setup(c => c.GetCartByIdAsync(5)).ReturnsAsync(new CartDTO
            {
                CartId = 5,
                UserId = 7,
                BasicSiteId = null,
                CartItems = new List<CartItemDTO> { new CartItemDTO { ProductId = 1 } }
            });

            var svc = CreateService();
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AddOrderFromCartAsync(5));
        }

        [Fact]
        public async Task AddOrderFromCartAsync_ProductMissing_ThrowsKeyNotFound()
        {
            _mockCartService.Setup(c => c.GetCartByIdAsync(5)).ReturnsAsync(new CartDTO
            {
                CartId = 5,
                UserId = 7,
                BasicSiteId = 2,
                BasicSitePrice = 15,
                TotalPrice = 25,
                CartItems = new List<CartItemDTO>
                {
                    new CartItemDTO { ProductId = 999, PlatformId = 1 }
                }
            });
            _mockProductRepo.Setup(p => p.GetProductByIdAsync(999)).ReturnsAsync((Product?)null);

            var svc = CreateService();
            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.AddOrderFromCartAsync(5));
        }

        [Fact]
        public async Task AddOrderFromCartAsync_NullPlatformId_DefaultsToOneInOrderItems()
        {
            Order? capturedOrder = null;
            _mockOrderRepo.Setup(r => r.AddOrderAsync(It.IsAny<Order>()))
                .Callback<Order>(order => capturedOrder = order)
                .ReturnsAsync((Order order) =>
                {
                    order.OrderId = 42;
                    return order;
                });

            _mockMapper.Setup(m => m.Map<OrderDetailsDTO>(It.IsAny<Order>()))
                .Returns((Order order) => new OrderDetailsDTO { OrderId = order.OrderId, OrderSum = order.OrderSum });

            _mockCartService.Setup(c => c.GetCartByIdAsync(5)).ReturnsAsync(new CartDTO
            {
                CartId = 5,
                UserId = 7,
                BasicSiteId = 2,
                BasicSitePrice = 10,
                TotalPrice = 40,
                CartItems = new List<CartItemDTO>
                {
                    new CartItemDTO { ProductId = 1, PlatformId = null },
                    new CartItemDTO { ProductId = 2, PlatformId = 3 }
                }
            });

            _mockProductRepo.Setup(p => p.GetProductByIdAsync(1)).ReturnsAsync(new Product { ProductId = 1, Price = 10 });
            _mockProductRepo.Setup(p => p.GetProductByIdAsync(2)).ReturnsAsync(new Product { ProductId = 2, Price = 20 });
            _mockPromptBuilder.Setup(p => p.BuildPromptAsync(It.IsAny<long>(), It.IsAny<IEnumerable<OrderItem>>()))
                .ReturnsAsync("test prompt");

            var svc = CreateService();
            await svc.AddOrderFromCartAsync(5);

            Assert.NotNull(capturedOrder);
            Assert.Equal(1, capturedOrder!.OrderItems.First().PlatformId);
        }

        [Fact]
        public async Task AddOrderFromCartAsync_SucceedsAndClearsCart()
        {
            const int cartId = 5;

            _mockProductRepo.Setup(p => p.GetProductByIdAsync(1)).ReturnsAsync(new Product { ProductId = 1, Price = 10 });
            _mockProductRepo.Setup(p => p.GetProductByIdAsync(2)).ReturnsAsync(new Product { ProductId = 2, Price = 20 });

            var cartDto = new CartDTO { CartId = 5, UserId = 7, BasicSitePrice = 15, TotalPrice = 45, CartItems = new List<CartItemDTO>
            {
                new CartItemDTO { ProductId = 1, PlatformId = 1, UserDescription = "a" },
                new CartItemDTO { ProductId = 2, PlatformId = 1, UserDescription = "b" }
            }, BasicSiteId = 2 };

            _mockCartService.Setup(c => c.GetCartByIdAsync(cartId)).ReturnsAsync(cartDto);

            var createdOrder = new Order { OrderId = 42, OrderSum = 45 };
            _mockOrderRepo.Setup(r => r.AddOrderAsync(It.IsAny<Order>())).ReturnsAsync(createdOrder);

            var expectedDto = new OrderDetailsDTO { OrderId = 42, OrderSum = 45 };
            _mockMapper.Setup(m => m.Map<OrderDetailsDTO>(It.IsAny<Order>())).Returns(expectedDto);
            _mockPromptBuilder.Setup(p => p.BuildPromptAsync(It.IsAny<long>(), It.IsAny<IEnumerable<OrderItem>>()))
                .ReturnsAsync("test prompt");

            var svc = CreateService();
            var result = await svc.AddOrderFromCartAsync(cartId);

            _mockOrderRepo.Verify(r => r.AddOrderAsync(It.IsAny<Order>()), Times.Once);
            _mockCartService.Verify(c => c.ClearCartAsync(cartId), Times.Once);
            Assert.Equal(expectedDto.OrderId, result.OrderId);
            Assert.Equal(expectedDto.OrderSum, result.OrderSum);
        }

        [Fact]
        public async Task AddOrderFromCartAsync_EmptyCart_Throws()
        {
            _mockCartService.Setup(c => c.GetCartByIdAsync(5)).ReturnsAsync(new CartDTO
            {
                CartId = 5,
                UserId = 7,
                BasicSiteId = 2,
                CartItems = new List<CartItemDTO>()
            });

            var svc = CreateService();
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AddOrderFromCartAsync(5));

            _mockOrderRepo.Verify(r => r.AddOrderAsync(It.IsAny<Order>()), Times.Never);
            _mockCartService.Verify(c => c.ClearCartAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task AddOrderFromCartAsync_TotalMismatch_Throws()
        {
            const int cartId = 5;

            _mockProductRepo.Setup(p => p.GetProductByIdAsync(1)).ReturnsAsync(new Product { ProductId = 1, Price = 10 });
            _mockProductRepo.Setup(p => p.GetProductByIdAsync(2)).ReturnsAsync(new Product { ProductId = 2, Price = 20 });

            _mockCartService.Setup(c => c.GetCartByIdAsync(cartId)).ReturnsAsync(new CartDTO
            {
                CartId = cartId,
                UserId = 7,
                BasicSiteId = 2,
                BasicSitePrice = 15,
                TotalPrice = 999,
                CartItems = new List<CartItemDTO>
                {
                    new CartItemDTO { ProductId = 1, PlatformId = 1 },
                    new CartItemDTO { ProductId = 2, PlatformId = 1 }
                }
            });

            var svc = CreateService();
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AddOrderFromCartAsync(cartId));

            _mockOrderRepo.Verify(r => r.AddOrderAsync(It.IsAny<Order>()), Times.Never);
            _mockCartService.Verify(c => c.ClearCartAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsNull()
        {
            _mockOrderRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Order?)null);
            var svc = CreateService();

            var res = await svc.GetByIdAsync(123);
            Assert.Null(res);
        }

        [Fact]
        public async Task UpdateStatusAsync_CallsRepository()
        {
            var dto = new OrderSummaryDTO { OrderId = 9, OrderSum = 10, StatusName = "In Progress" };
            var existingOrder = new Order { OrderId = 9, Status = 1 };

            _mockOrderRepo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(existingOrder);
            _mockOrderRepo.Setup(r => r.GetStatusesAsync()).ReturnsAsync(new List<Status>
            {
                new Status { StatusId = 2, StatusName = "In Progress" }
            });

            var svc = CreateService();
            await svc.UpdateStatusAsync(dto);

            _mockOrderRepo.Verify(r => r.UpdateStatusAsync(It.Is<Order>(o => o.OrderId == 9 && o.Status == 2)), Times.Once);
        }
    }
}
