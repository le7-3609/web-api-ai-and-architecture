using Moq;
using Moq.EntityFrameworkCore;
using Repositories;
using Entities;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace Tests.UnitTests
{
    public class ProductRepositoryUnitTesting
    {
        private readonly Mock<MyShopContext> _mockContext;
        private readonly ProductRepository _repo;

        public ProductRepositoryUnitTesting()
        {
            _mockContext = new Mock<MyShopContext>();
            _repo = new ProductRepository(_mockContext.Object);
        }

        private Product CreateValidProduct(int id, string name = "Test") =>
            new Product { ProductId = id, ProductName = name, ProductPrompt = "Required Content", SubCategoryId = 1 };

        private void MockDbSets(List<Product> products, List<CartItem> carts, List<OrderItem> orders)
        {
            _mockContext.Setup(x => x.Products).ReturnsDbSet(products);
            _mockContext.Setup(x => x.CartItems).ReturnsDbSet(carts);
            _mockContext.Setup(x => x.OrderItems).ReturnsDbSet(orders);
        }

        #region Happy Paths

        [Fact]
        public async Task AddProductAsync_ValidProduct_ReturnsProduct()
        {
            MockDbSets(new List<Product>(), new List<CartItem>(), new List<OrderItem>());
            var product = CreateValidProduct(0, "New Product");

            var result = await _repo.AddProductAsync(product);

            Assert.Equal("New Product", result.ProductName);
            _mockContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DeleteProductAsync_ProductExistsAndNoLinks_ReturnsTrue()
        {
            var product = CreateValidProduct(10);
            MockDbSets(new List<Product> { product }, new List<CartItem>(), new List<OrderItem>());

            var result = await _repo.DeleteProductAsync(10);

            Assert.True(result);
            _mockContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task DeleteProductAsync_NonExistentProduct_ReturnsFalse()
        {
            MockDbSets(new List<Product>(), new List<CartItem>(), new List<OrderItem>());

            var result = await _repo.DeleteProductAsync(999);

            Assert.False(result);
            _mockContext.Verify(m => m.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task UpdateProductAsync_InvalidId_StillCallsSave()
        {
            MockDbSets(new List<Product>(), new List<CartItem>(), new List<OrderItem>());
            var product = CreateValidProduct(999);

            await _repo.UpdateProductAsync(999, product);

            _mockContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        #endregion
    }
}