using AutoMapper;
using DTO;
using Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using Repositories;
using Services;

namespace Tests.UnitTests
{
    public class CartServiceUnitTests
    {
        [Fact]
        public async Task AddCartItemForUserAsync_WhenProductAlreadyExists_ThrowsInvalidOperationException()
        {
            var repository = new Mock<ICartRepository>();
            var basicSiteService = new Mock<IBasicSiteService>();
            var mapper = new Mock<IMapper>();

            repository.Setup(r => r.UserExistsAsync(4)).ReturnsAsync(true);
            repository.Setup(r => r.ProductExistsAsync(8)).ReturnsAsync(true);
            repository.Setup(r => r.GetCartByUserIdAsync(4)).ReturnsAsync(new Cart { CartId = 20, UserId = 4, BasicSiteId = 1 });
            repository.Setup(r => r.GetByCartAndProductIdAsync(20, 8)).ReturnsAsync(new CartItem { CartItemId = 1, CartId = 20, ProductId = 8 });

            var service = new CartService(repository.Object, basicSiteService.Object, mapper.Object);
            var dto = new AddCartItemDTO(0, 8, 2);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddCartItemForUserAsync(4, dto));
        }

        [Fact]
        public async Task AddCartItemForUserAsync_InvalidUserId_ThrowsInvalidOperationException()
        {
            var repository = new Mock<ICartRepository>();
            var basicSiteService = new Mock<IBasicSiteService>();
            var mapper = new Mock<IMapper>();

            var service = new CartService(repository.Object, basicSiteService.Object, mapper.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AddCartItemForUserAsync(0, new AddCartItemDTO(0, 8, 2)));
        }

        [Fact]
        public async Task AddCartItemForUserAsync_UserDoesNotExist_ThrowsInvalidOperationException()
        {
            var repository = new Mock<ICartRepository>();
            var basicSiteService = new Mock<IBasicSiteService>();
            var mapper = new Mock<IMapper>();

            repository.Setup(r => r.ProductExistsAsync(8)).ReturnsAsync(true);
            repository.Setup(r => r.UserExistsAsync(4)).ReturnsAsync(false);

            var service = new CartService(repository.Object, basicSiteService.Object, mapper.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AddCartItemForUserAsync(4, new AddCartItemDTO(0, 8, 2)));
        }

        [Fact]
        public async Task AddCartItemForUserAsync_WhenRepositoryThrowsDuplicateDbUpdate_ThrowsInvalidOperationException()
        {
            var repository = new Mock<ICartRepository>();
            var basicSiteService = new Mock<IBasicSiteService>();
            var mapper = new Mock<IMapper>();

            repository.Setup(r => r.ProductExistsAsync(8)).ReturnsAsync(true);
            repository.Setup(r => r.UserExistsAsync(4)).ReturnsAsync(true);
            repository.Setup(r => r.GetCartByUserIdAsync(4))
                .ReturnsAsync(new Cart { CartId = 20, UserId = 4, BasicSiteId = null });
            repository.Setup(r => r.GetByCartAndProductIdAsync(20, 8)).ReturnsAsync((CartItem)null!);
            repository.Setup(r => r.AddCartItemAsync(It.IsAny<CartItem>()))
                .ThrowsAsync(new DbUpdateException("duplicate", new Exception("duplicate key")));

            var service = new CartService(repository.Object, basicSiteService.Object, mapper.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AddCartItemForUserAsync(4, new AddCartItemDTO(0, 8, 2)));
        }

        [Fact]
        public async Task AddCartItemForUserAsync_WhenNoCart_CreatesCartAndReturnsMappedItem()
        {
            var repository = new Mock<ICartRepository>();
            var basicSiteService = new Mock<IBasicSiteService>();
            var mapper = new Mock<IMapper>();

            repository.Setup(r => r.UserExistsAsync(4)).ReturnsAsync(true);
            repository.Setup(r => r.ProductExistsAsync(8)).ReturnsAsync(true);
            repository.Setup(r => r.GetCartByUserIdAsync(4)).ReturnsAsync((Cart)null!);
            repository.Setup(r => r.CreateUserCartAsync(It.IsAny<Cart>())).ReturnsAsync(new Cart { CartId = 30, UserId = 4, BasicSiteId = null });
            repository.Setup(r => r.GetByCartAndProductIdAsync(30, 8)).ReturnsAsync((CartItem)null!);

            var createdItem = new CartItem { CartItemId = 9, CartId = 30, ProductId = 8, PlatformId = 2 };
            repository.Setup(r => r.AddCartItemAsync(It.IsAny<CartItem>())).ReturnsAsync(createdItem);
            mapper.Setup(m => m.Map<CartItemDTO>(createdItem)).Returns(new CartItemDTO { CartItemId = 9, CartId = 30, ProductId = 8, PlatformId = 2 });

            var service = new CartService(repository.Object, basicSiteService.Object, mapper.Object);
            var dto = new AddCartItemDTO(0, 8, 2);

            var result = await service.AddCartItemForUserAsync(4, dto);

            Assert.Equal(9, result.CartItemId);
            repository.Verify(r => r.CreateUserCartAsync(It.IsAny<Cart>()), Times.Once);
            repository.Verify(r => r.AddCartItemAsync(It.IsAny<CartItem>()), Times.Once);
        }

        [Fact]
        public async Task GetCartByIdAsync_ComputesTotalWithBasicSitePrice()
        {
            var repository = new Mock<ICartRepository>();
            var basicSiteService = new Mock<IBasicSiteService>();
            var mapper = new Mock<IMapper>();

            repository.Setup(r => r.GetCartByIdAsync(5))
                .ReturnsAsync(new Cart { CartId = 5, UserId = 3, BasicSiteId = 7 });
            repository.Setup(r => r.GetCartItemsByCartIdAsync(5))
                .ReturnsAsync(new List<CartItem> { new CartItem(), new CartItem() });
            mapper.Setup(m => m.Map<List<CartItemDTO>>(It.IsAny<IEnumerable<CartItem>>()))
                .Returns(new List<CartItemDTO>
                {
                    new CartItemDTO { Price = 10 },
                    new CartItemDTO { Price = 15 }
                });
            basicSiteService.Setup(s => s.GetBasicSitePriceAsync(7)).ReturnsAsync(5);

            var service = new CartService(repository.Object, basicSiteService.Object, mapper.Object);
            var result = await service.GetCartByIdAsync(5);

            Assert.NotNull(result);
            Assert.Equal(5, result!.BasicSitePrice);
            Assert.Equal(30, result.TotalPrice);
        }

        [Fact]
        public async Task GetCartByIdAsync_WithNullBasicSiteId_UsesZeroBasicSitePrice()
        {
            var repository = new Mock<ICartRepository>();
            var basicSiteService = new Mock<IBasicSiteService>();
            var mapper = new Mock<IMapper>();

            repository.Setup(r => r.GetCartByIdAsync(9))
                .ReturnsAsync(new Cart { CartId = 9, UserId = 2, BasicSiteId = null });
            repository.Setup(r => r.GetCartItemsByCartIdAsync(9))
                .ReturnsAsync(new List<CartItem> { new CartItem() });
            mapper.Setup(m => m.Map<List<CartItemDTO>>(It.IsAny<IEnumerable<CartItem>>()))
                .Returns(new List<CartItemDTO> { new CartItemDTO { Price = 12 } });

            var service = new CartService(repository.Object, basicSiteService.Object, mapper.Object);
            var result = await service.GetCartByIdAsync(9);

            Assert.NotNull(result);
            Assert.Equal(0, result!.BasicSitePrice);
            Assert.Equal(12, result.TotalPrice);
            basicSiteService.Verify(s => s.GetBasicSitePriceAsync(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task UpdateCartAsync_ComputesTotalFromItemsAndBasicSitePrice()
        {
            var repository = new Mock<ICartRepository>();
            var basicSiteService = new Mock<IBasicSiteService>();
            var mapper = new Mock<IMapper>();

            repository.Setup(r => r.BasicSiteExistsAsync(3)).ReturnsAsync(true);
            repository.Setup(r => r.UpdateCartAsync(It.IsAny<Cart>()))
                .ReturnsAsync(new Cart { CartId = 5, UserId = 4, BasicSiteId = 3 });
            repository.Setup(r => r.GetCartItemsByCartIdAsync(5))
                .ReturnsAsync(new List<CartItem> { new CartItem(), new CartItem() });
            mapper.Setup(m => m.Map<List<CartItemDTO>>(It.IsAny<IEnumerable<CartItem>>()))
                .Returns(new List<CartItemDTO>
                {
                    new CartItemDTO { Price = 2 },
                    new CartItemDTO { Price = 8 }
                });
            basicSiteService.Setup(s => s.GetBasicSitePriceAsync(3)).ReturnsAsync(7);

            var service = new CartService(repository.Object, basicSiteService.Object, mapper.Object);
            var result = await service.UpdateCartAsync(5, new UpdateCartDTO(3));

            Assert.NotNull(result);
            Assert.Equal(7, result!.BasicSitePrice);
            Assert.Equal(17, result.TotalPrice);
        }

    }
}
