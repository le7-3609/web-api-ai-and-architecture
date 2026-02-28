using Entities;
using Repositories;
using Xunit;

namespace Tests.IntegrationTests
{
    [Collection("Database collection")]
    public class MainCategoryRepositoryIntegrationTests
    {
        private readonly DatabaseFixture _fixture;
        private readonly MyShopContext _context;
        private readonly MainCategoryRepository _repository;

        public MainCategoryRepositoryIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Context;
            _repository = new MainCategoryRepository(_context);
            _fixture.ClearDatabase();
        }

        [Fact]
        public async Task AddMainCategoryAsync_Adds()
        {
            var mc = new MainCategory { MainCategoryName = "NewMain", MainCategoryPrompt = "p" };
            var saved = await _repository.AddMainCategoryAsync(mc);
            Assert.True(saved.MainCategoryId > 0);
        }

        [Fact]
        public async Task HasSubCategoriesAsync_ReturnsTrue_WhenHasSubcategories()
        {
            var mc = new MainCategory { MainCategoryId = 200, MainCategoryName = "X" };
            _context.MainCategories.Add(mc);
            _context.SubCategories.Add(new SubCategory { SubCategoryId = 300, MainCategoryId = 200, SubCategoryName = "S" });
            _context.SaveChanges();

            var result = await _repository.HasSubCategoriesAsync(200);
            Assert.True(result);
        }

        [Fact]
        public async Task HasSubCategoriesAsync_ReturnsFalse_WhenNoSubcategories()
        {
            var mc = new MainCategory { MainCategoryId = 202, MainCategoryName = "Z" };
            _context.MainCategories.Add(mc);
            _context.SaveChanges();

            var result = await _repository.HasSubCategoriesAsync(202);
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteMainCategoryAsync_Succeeds_WhenNoSubcategories()
        {
            var mc = new MainCategory { MainCategoryId = 201, MainCategoryName = "Y" };
            _context.MainCategories.Add(mc);
            _context.SaveChanges();

            var result = await _repository.DeleteMainCategoryAsync(201);
            Assert.True(result);
        }
    }
}
