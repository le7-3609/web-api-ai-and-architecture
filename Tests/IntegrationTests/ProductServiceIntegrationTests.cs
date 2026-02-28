using Xunit;
using Entities;
using Repositories;
using Services;
using AutoMapper;
using DTO;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.IntegrationTests
{
    [Collection("Database collection")]
    public class ProductServiceIntegrationTests
    {
        private readonly DatabaseFixture _fixture;
        private readonly MyShopContext _context;
        private readonly ProductService _productService;
        private readonly IMapper _mapper;

        public ProductServiceIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Context;
            _fixture.ClearDatabase();

            var services = new ServiceCollection();
            services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Services.Mapper).Assembly));
            var serviceProvider = services.BuildServiceProvider();
            _mapper = serviceProvider.GetRequiredService<IMapper>();

            var productRepo = new ProductRepository(_context);
            _productService = new ProductService(productRepo, _mapper);
        }

        [Fact]
        public async Task AddProductAsync_WithValidProduct_CreatesProduct()
        {
            // Arrange
            var mainCat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Main", MainCategoryPrompt = "P" };
            _context.MainCategories.Add(mainCat);

            var subCat = new SubCategory { SubCategoryId = 1, MainCategoryId = 1, SubCategoryName = "Sub", SubCategoryPrompt = "P" };
            _context.SubCategories.Add(subCat);
            await _context.SaveChangesAsync();

            var dto = new AdminProductDTO(null, 1, "NewProduct", 100, "Prompt");

            // Act
            var result = await _productService.AddProductAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("NewProduct", result.ProductName);
            Assert.True(result.ProductId > 0);
        }

        [Fact]
        public async Task UpdateProductAsync_WithValidProduct_UpdatesProduct()
        {
            // Arrange
            var mainCat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Main", MainCategoryPrompt = "P" };
            _context.MainCategories.Add(mainCat);

            var subCat = new SubCategory { SubCategoryId = 1, MainCategoryId = 1, SubCategoryName = "Sub", SubCategoryPrompt = "P" };
            _context.SubCategories.Add(subCat);

            var product = new Product { ProductId = 1, SubCategoryId = 1, ProductName = "OldName", ProductPrompt = "P", Price = 50 };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var dto = new AdminProductDTO(1, 1, "UpdatedName", 150, "UpdatedPrompt");

            // Act
            await _productService.UpdateProductAsync(1, dto);

            // Assert
            var updated = await _context.Products.FindAsync(1L);
            Assert.NotNull(updated);
            Assert.Equal("UpdatedName", updated.ProductName);
        }
    }
}
