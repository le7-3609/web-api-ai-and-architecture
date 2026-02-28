using System.Threading.Tasks;
using AutoMapper;
using DTO;
using Entities;
using Moq;
using Repositories;
using Services;
using Xunit;

namespace Tests.UnitTests
{
    public class ProductServiceUnitTests
    {
        [Fact]
        public async Task AddProductAsync_Throws_OnEmptyName()
        {
            var mockRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            var svc = new ProductService(mockRepo.Object, mockMapper.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => svc.AddProductAsync(new AdminProductDTO(null, 1, "", 10, "p")));
        }

        [Fact]
        public async Task AddProductAsync_SavesAndReturnsDto()
        {
            var mockRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();

            var dto = new AdminProductDTO(null, 1, "Widget", 10, "p");
            var mapped = new Product { ProductName = "Widget" };
            mockMapper.Setup(m => m.Map<Product>(dto)).Returns(mapped);

            var saved = new Product { ProductId = 88, ProductName = "Widget", ProductPrompt = "p" };
            mockRepo.Setup(r => r.AddProductAsync(It.IsAny<Product>())).ReturnsAsync(saved);
            var outDto = new ProductDTO(88, 1, "Widget", "Sub", 10, "p");
            mockMapper.Setup(m => m.Map<ProductDTO>(saved)).Returns(outDto);

            var svc = new ProductService(mockRepo.Object, mockMapper.Object);

            var res = await svc.AddProductAsync(dto);

            mockRepo.Verify(r => r.AddProductAsync(It.IsAny<Product>()), Times.Once);
            Assert.Equal(outDto.ProductId, res.ProductId);
        }

        [Fact]
        public async Task DeleteProductAsync_WhenNotFound_ReturnsFalse()
        {
            var mockRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            mockRepo.Setup(r => r.GetProductByIdAsync(99)).ReturnsAsync((Product)null!);

            var svc = new ProductService(mockRepo.Object, mockMapper.Object);

            var result = await svc.DeleteProductAsync(99);

            Assert.False(result);
            mockRepo.Verify(r => r.DeleteProductAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteProductAsync_WithOrders_ThrowsInvalidOperationException()
        {
            var mockRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            mockRepo.Setup(r => r.GetProductByIdAsync(1)).ReturnsAsync(new Product { ProductId = 1, ProductName = "P" });
            mockRepo.Setup(r => r.HasOrderItemsByProductIdAsync(1)).ReturnsAsync(true);

            var svc = new ProductService(mockRepo.Object, mockMapper.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeleteProductAsync(1));
            Assert.Contains("orders", ex.Message, StringComparison.OrdinalIgnoreCase);
            mockRepo.Verify(r => r.DeleteProductAsync(It.IsAny<int>()), Times.Never);
            mockRepo.Verify(r => r.RemoveCartItemsByProductIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteProductAsync_WithCartItems_RemovesCartItemsThenDeletes()
        {
            var mockRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            mockRepo.Setup(r => r.GetProductByIdAsync(1)).ReturnsAsync(new Product { ProductId = 1, ProductName = "P" });
            mockRepo.Setup(r => r.HasOrderItemsByProductIdAsync(1)).ReturnsAsync(false);
            mockRepo.Setup(r => r.RemoveCartItemsByProductIdAsync(1)).Returns(Task.CompletedTask);
            mockRepo.Setup(r => r.DeleteProductAsync(1)).ReturnsAsync(true);

            var svc = new ProductService(mockRepo.Object, mockMapper.Object);

            var result = await svc.DeleteProductAsync(1);

            Assert.True(result);
            mockRepo.Verify(r => r.RemoveCartItemsByProductIdAsync(1), Times.Once);
            mockRepo.Verify(r => r.DeleteProductAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteProductAsync_NoOrdersNoCartItems_DeletesSuccessfully()
        {
            var mockRepo = new Mock<IProductRepository>();
            var mockMapper = new Mock<IMapper>();
            mockRepo.Setup(r => r.GetProductByIdAsync(1)).ReturnsAsync(new Product { ProductId = 1, ProductName = "P" });
            mockRepo.Setup(r => r.HasOrderItemsByProductIdAsync(1)).ReturnsAsync(false);
            mockRepo.Setup(r => r.RemoveCartItemsByProductIdAsync(1)).Returns(Task.CompletedTask);
            mockRepo.Setup(r => r.DeleteProductAsync(1)).ReturnsAsync(true);

            var svc = new ProductService(mockRepo.Object, mockMapper.Object);

            var result = await svc.DeleteProductAsync(1);

            Assert.True(result);
        }
    }
}
