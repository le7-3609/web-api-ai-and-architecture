using Entities;
using Repositories;
using Xunit;

namespace Tests.IntegrationTests
{
    [Collection("Database collection")]
    public class ProductRepositoryIntegrationTests
    {
        private readonly DatabaseFixture _fixture;
        private readonly MyShopContext _context;
        private readonly ProductRepository _repository;

        public ProductRepositoryIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Context;
            _repository = new ProductRepository(_context);
            _fixture.ClearDatabase();
        }

        private void SeedMainAndSubCategory()
        {
            var mainCat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Main", MainCategoryPrompt = "P" };
            _context.MainCategories.Add(mainCat);
            var subCat = new SubCategory { SubCategoryId = 1, SubCategoryName = "Sub", SubCategoryPrompt = "P", MainCategoryId = 1 };
            _context.SubCategories.Add(subCat);
            _context.SaveChanges();
        }

        #region Happy Paths

        [Fact]
        public async Task AddProductAsync_ValidProduct_ReturnsProduct()
        {
            SeedMainAndSubCategory();
            var product = new Product { ProductName = "Laptop", ProductPrompt = "Required", SubCategoryId = 1 };

            var result = await _repository.AddProductAsync(product);

            Assert.True(result.ProductId > 0);
            Assert.Equal("Laptop", result.ProductName);
        }

        [Fact]
        public async Task GetProductByIdAsync_WithValidId_ReturnsProduct()
        {
            SeedMainAndSubCategory();
            var product = new Product { ProductId = 5, ProductName = "Phone", ProductPrompt = "P", SubCategoryId = 1 };
            _context.Products.Add(product);
            _context.SaveChanges();

            var result = await _repository.GetProductByIdAsync(5);

            Assert.NotNull(result);
            Assert.Equal("Phone", result.ProductName);
        }

        [Fact]
        public async Task GetProductsAsync_FilterBySubCategory_ReturnsFiltered()
        {
            SeedMainAndSubCategory();
            _context.Products.AddRange(
                new Product { ProductName = "P1", ProductPrompt = "P", SubCategoryId = 1 },
                new Product { ProductName = "P2", ProductPrompt = "P", SubCategoryId = 1 }
            );
            _context.SaveChanges();

            var (products, total) = await _repository.GetProductsAsync(10, 0, null, new int?[] { 1 });

            Assert.Equal(2, products.Count());
            Assert.Equal(2, total);
        }

        [Fact]
        public async Task UpdateProductAsync_WithValidProduct_Updates()
        {
            SeedMainAndSubCategory();
            var product = new Product { ProductId = 10, ProductName = "Old", ProductPrompt = "P", SubCategoryId = 1 };
            _context.Products.Add(product);
            _context.SaveChanges();

            product.ProductName = "New";
            await _repository.UpdateProductAsync(10, product);

            var updated = _context.Products.Find(10L);
            Assert.NotNull(updated);
            Assert.Equal("New", updated.ProductName);
        }

        [Fact]
        public async Task GetProductsBySubCategoryIdAsync_ReturnsCorrectProducts()
        {
            SeedMainAndSubCategory();
            _context.Products.AddRange(
                new Product { ProductName = "P1", ProductPrompt = "P", SubCategoryId = 1 },
                new Product { ProductName = "P2", ProductPrompt = "P", SubCategoryId = 1 }
            );
            _context.SaveChanges();

            var result = await _repository.GetProductsBySubCategoryIdAsync(1);

            Assert.Equal(2, result.Count());
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task GetProductByIdAsync_NonExistentId_ReturnsNull()
        {
            var result = await _repository.GetProductByIdAsync(9999);
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteProductAsync_NonExistentProduct_ReturnsFalse()
        {
            var result = await _repository.DeleteProductAsync(9999);
            Assert.False(result);
        }

        [Fact]
        public async Task GetProductsAsync_EmptyDatabase_ReturnsEmpty()
        {
            var (products, total) = await _repository.GetProductsAsync(10, 0, null, Array.Empty<int?>());

            Assert.Empty(products);
            Assert.Equal(0, total);
        }

        [Fact]
        public async Task GetProductsBySubCategoryIdAsync_NonExistentSubCategory_ReturnsEmpty()
        {
            var result = await _repository.GetProductsBySubCategoryIdAsync(9999);

            Assert.Empty(result);
        }

        [Fact]
        public async Task HasOrderItemsByProductIdAsync_WithOrders_ReturnsTrue()
        {
            SeedMainAndSubCategory();
            var product = new Product { ProductId = 30, ProductName = "P", ProductPrompt = "P", SubCategoryId = 1 };
            _context.Products.Add(product);

            var platform = new Platform { PlatformId = 1, PlatformName = "Plat" };
            _context.Platforms.Add(platform);

            var siteType = new SiteType { SiteTypeId = 1, SiteTypeName = "T" };
            _context.SiteTypes.Add(siteType);

            var basicSite = new BasicSite { BasicSiteId = 1, SiteName = "S", SiteTypeId = 1 };
            _context.BasicSites.Add(basicSite);

            var user = new User
            {
                UserId = 1,
                Email = "u@u.com",
                Password = "p",
                FirstName = "U",
                LastName = "L",
                Phone = "0500000000",
                Provider = "Local",
                ProviderId = "1"
            };
            _context.Users.Add(user);

            var order = new Order { OrderId = 1, BasicSiteId = 1, UserId = 1, Status = 1 };
            _context.Orders.Add(order);

            var orderItem = new OrderItem { OrderItemId = 1, ProductId = 30, OrderId = 1, PlatformId = 1 };
            _context.OrderItems.Add(orderItem);
            _context.SaveChanges();

            var result = await _repository.HasOrderItemsByProductIdAsync(30);

            Assert.True(result);
        }

        [Fact]
        public async Task HasOrderItemsByProductIdAsync_WithNoOrders_ReturnsFalse()
        {
            SeedMainAndSubCategory();
            var product = new Product { ProductId = 31, ProductName = "P", ProductPrompt = "P", SubCategoryId = 1 };
            _context.Products.Add(product);
            _context.SaveChanges();

            var result = await _repository.HasOrderItemsByProductIdAsync(31);

            Assert.False(result);
        }

        [Fact]
        public async Task RemoveCartItemsByProductIdAsync_RemovesMatchingCartItems()
        {
            SeedMainAndSubCategory();
            var product = new Product { ProductId = 40, ProductName = "P", ProductPrompt = "P", SubCategoryId = 1 };
            _context.Products.Add(product);

            var user = new User
            {
                UserId = 1,
                Email = "u@u.com",
                Password = "p",
                FirstName = "U",
                LastName = "L",
                Phone = "0500000000",
                Provider = "Local",
                ProviderId = "1"
            };
            _context.Users.Add(user);

            var cart = new Cart { CartId = 1, UserId = 1 };
            _context.Carts.Add(cart);

            _context.CartItems.AddRange(
                new CartItem { CartItemId = 1, CartId = 1, ProductId = 40 },
                new CartItem { CartItemId = 2, CartId = 1, ProductId = 40 }
            );
            _context.SaveChanges();

            await _repository.RemoveCartItemsByProductIdAsync(40);

            var remaining = _context.CartItems.Where(ci => ci.ProductId == 40).ToList();
            Assert.Empty(remaining);
        }

        [Fact]
        public async Task RemoveCartItemsByProductIdAsync_NoCartItems_CompletesWithoutError()
        {
            SeedMainAndSubCategory();
            var product = new Product { ProductId = 41, ProductName = "P", ProductPrompt = "P", SubCategoryId = 1 };
            _context.Products.Add(product);
            _context.SaveChanges();

            await _repository.RemoveCartItemsByProductIdAsync(41);

            // Should complete without error
        }

        #endregion
    }
}