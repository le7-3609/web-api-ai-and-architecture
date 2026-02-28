using Entities;
using Repositories;
using Tests.IntegrationTests;
using Xunit;

namespace Tests.IntegretionTests
{
    [Collection("Database collection")]
    public class SubCategoryRepositoryIntegrationTests
    {
        private readonly DatabaseFixture _fixture;
        private readonly MyShopContext _context;
        private readonly SubCategoryRepository _repository;

        public SubCategoryRepositoryIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Context;
            _repository = new SubCategoryRepository(_context);
            _fixture.ClearDatabase();
        }

        private void SeedMainCategory()
        {
            var mainCat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Main", MainCategoryPrompt = "P" };
            _context.MainCategories.Add(mainCat);
            _context.SaveChanges();
        }

        #region Happy Paths

        [Fact]
        public async Task AddSubCategoryAsync_ValidSubCategory_ReturnsSubCategory()
        {
            SeedMainCategory();
            var subCat = new SubCategory { SubCategoryName = "Electronics", SubCategoryPrompt = "P", MainCategoryId = 1 };

            var result = await _repository.AddSubCategoryAsync(subCat);

            Assert.True(result.SubCategoryId > 0);
            Assert.Equal("Electronics", result.SubCategoryName);
        }

        [Fact]
        public async Task GetSubCategoryByIdAsync_WithValidId_ReturnsSubCategory()
        {
            SeedMainCategory();
            var subCat = new SubCategory { SubCategoryId = 5, SubCategoryName = "Books", SubCategoryPrompt = "P", MainCategoryId = 1 };
            _context.SubCategories.Add(subCat);
            _context.SaveChanges();

            var result = await _repository.GetSubCategoryByIdAsync(5);

            Assert.NotNull(result);
            Assert.Equal("Books", result.SubCategoryName);
        }

        [Fact]
        public async Task GetByNameAsync_WithValidName_ReturnsSubCategory()
        {
            SeedMainCategory();
            var subCat = new SubCategory { SubCategoryName = "Unique", SubCategoryPrompt = "P", MainCategoryId = 1 };
            _context.SubCategories.Add(subCat);
            _context.SaveChanges();

            var result = await _repository.GetByNameAsync("Unique");

            Assert.NotNull(result);
            Assert.Equal("Unique", result.SubCategoryName);
        }

        [Fact]
        public async Task MainCategoryExistsAsync_WithExistingMainCategory_ReturnsTrue()
        {
            SeedMainCategory();

            var result = await _repository.MainCategoryExistsAsync(1);

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateSubCategoryAsync_WithValidSubCategory_Updates()
        {
            SeedMainCategory();
            var subCat = new SubCategory { SubCategoryId = 10, SubCategoryName = "Old", SubCategoryPrompt = "P", MainCategoryId = 1 };
            _context.SubCategories.Add(subCat);
            _context.SaveChanges();

            subCat.SubCategoryName = "Updated";
            await _repository.UpdateSubCategoryAsync(10, subCat);

            var updated = _context.SubCategories.Find(10L);
            Assert.NotNull(updated);
            Assert.Equal("Updated", updated.SubCategoryName);
        }

        [Fact]
        public async Task GetSubCategoryAsync_FilterByMainCategory_ReturnsFiltered()
        {
            SeedMainCategory();
            _context.SubCategories.AddRange(
                new SubCategory { SubCategoryName = "S1", SubCategoryPrompt = "P", MainCategoryId = 1 },
                new SubCategory { SubCategoryName = "S2", SubCategoryPrompt = "P", MainCategoryId = 1 }
            );
            _context.SaveChanges();

            var (subCats, total) = await _repository.GetSubCategoryAsync(10, 0, null, new int?[] { 1 });

            Assert.Equal(2, subCats.Count());
            Assert.Equal(2, total);
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task GetSubCategoryByIdAsync_NonExistentId_ReturnsNull()
        {
            var result = await _repository.GetSubCategoryByIdAsync(9999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByNameAsync_NonExistentName_ReturnsNull()
        {
            var result = await _repository.GetByNameAsync("Nonexistent");

            Assert.Null(result);
        }

        [Fact]
        public async Task MainCategoryExistsAsync_WithNonExistentMainCategory_ReturnsFalse()
        {
            var result = await _repository.MainCategoryExistsAsync(9999);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteSubCategoryAsync_WithExistingSubCategory_DeletesSuccessfully()
        {
            SeedMainCategory();
            var subCat = new SubCategory { SubCategoryId = 20, SubCategoryName = "ToDelete", SubCategoryPrompt = "P", MainCategoryId = 1 };
            _context.SubCategories.Add(subCat);
            _context.SaveChanges();

            var result = await _repository.DeleteSubCategoryAsync(20);

            Assert.True(result);
            Assert.Null(await _repository.GetSubCategoryByIdAsync(20));
        }

        [Fact]
        public async Task DeleteSubCategoryAsync_NonExistentId_ReturnsFalse()
        {
            var result = await _repository.DeleteSubCategoryAsync(9999);

            Assert.False(result);
        }

        [Fact]
        public async Task HasProductsAsync_WithLinkedProducts_ReturnsTrue()
        {
            SeedMainCategory();
            var subCat = new SubCategory { SubCategoryId = 30, SubCategoryName = "HasProducts", SubCategoryPrompt = "P", MainCategoryId = 1 };
            _context.SubCategories.Add(subCat);
            var product = new Product { ProductId = 1, SubCategoryId = 30, ProductName = "P", ProductPrompt = "P" };
            _context.Products.Add(product);
            _context.SaveChanges();

            var result = await _repository.HasProductsAsync(30);

            Assert.True(result);
        }

        [Fact]
        public async Task HasProductsAsync_WithNoProducts_ReturnsFalse()
        {
            SeedMainCategory();
            var subCat = new SubCategory { SubCategoryId = 31, SubCategoryName = "Empty", SubCategoryPrompt = "P", MainCategoryId = 1 };
            _context.SubCategories.Add(subCat);
            _context.SaveChanges();

            var result = await _repository.HasProductsAsync(31);

            Assert.False(result);
        }

        #endregion
    }
}
