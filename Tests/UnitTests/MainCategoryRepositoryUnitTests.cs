using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using Xunit;

namespace Tests.UnitTests
{
    public class MainCategoryRepositoryUnitTests
    {
        private static MyShopContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<MyShopContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new MyShopContext(options);
        }

        #region Happy Paths

        [Fact]
        public async Task AddMainCategoryAsync_WithValidCategory_ReturnsCategory()
        {
            var ctx = CreateInMemoryContext(nameof(AddMainCategoryAsync_WithValidCategory_ReturnsCategory));
            var repo = new MainCategoryRepository(ctx);

            var cat = new MainCategory { MainCategoryName = "Electronics", MainCategoryPrompt = "P" };
            var result = await repo.AddMainCategoryAsync(cat);

            Assert.True(result.MainCategoryId > 0);
        }

        [Fact]
        public async Task GetMainCategoryByIdAsync_WithValidId_ReturnsCategory()
        {
            var ctx = CreateInMemoryContext(nameof(GetMainCategoryByIdAsync_WithValidId_ReturnsCategory));
            var repo = new MainCategoryRepository(ctx);

            var cat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Books", MainCategoryPrompt = "P" };
            await ctx.MainCategories.AddAsync(cat);
            await ctx.SaveChangesAsync();

            var result = await repo.GetMainCategoryByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Books", result.MainCategoryName);
        }

        [Fact]
        public async Task GetMainCategoriesAsync_ReturnsAllCategories()
        {
            var ctx = CreateInMemoryContext(nameof(GetMainCategoriesAsync_ReturnsAllCategories));
            var repo = new MainCategoryRepository(ctx);

            await ctx.MainCategories.AddRangeAsync(
                new MainCategory { MainCategoryName = "Cat1", MainCategoryPrompt = "P" },
                new MainCategory { MainCategoryName = "Cat2", MainCategoryPrompt = "P" }
            );
            await ctx.SaveChangesAsync();

            var result = await repo.GetMainCategoriesAsync();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task UpdateMainCategoryAsync_WithValidData_Updates()
        {
            var ctx = CreateInMemoryContext(nameof(UpdateMainCategoryAsync_WithValidData_Updates));
            var repo = new MainCategoryRepository(ctx);

            var cat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Old", MainCategoryPrompt = "P" };
            await ctx.MainCategories.AddAsync(cat);
            await ctx.SaveChangesAsync();

            cat.MainCategoryName = "New";
            await repo.UpdateMainCategoryAsync(cat);

            var updated = await ctx.MainCategories.FindAsync(1L);
            Assert.NotNull(updated);
            Assert.Equal("New", updated.MainCategoryName);
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task GetMainCategoryByIdAsync_NonExistentId_ReturnsNull()
        {
            var ctx = CreateInMemoryContext(nameof(GetMainCategoryByIdAsync_NonExistentId_ReturnsNull));
            var repo = new MainCategoryRepository(ctx);

            var result = await repo.GetMainCategoryByIdAsync(9999);

            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteMainCategoryAsync_NonExistentId_ReturnsFalse()
        {
            var ctx = CreateInMemoryContext(nameof(DeleteMainCategoryAsync_NonExistentId_ReturnsFalse));
            var repo = new MainCategoryRepository(ctx);

            var result = await repo.DeleteMainCategoryAsync(9999);

            Assert.False(result);
        }

        [Fact]
        public async Task HasSubCategoriesAsync_WithLinkedSubCategories_ReturnsTrue()
        {
            var ctx = CreateInMemoryContext(nameof(HasSubCategoriesAsync_WithLinkedSubCategories_ReturnsTrue));
            var repo = new MainCategoryRepository(ctx);

            var cat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Test", MainCategoryPrompt = "P" };
            await ctx.MainCategories.AddAsync(cat);
            var subCat = new SubCategory { SubCategoryName = "Sub", SubCategoryPrompt = "P", MainCategoryId = 1 };
            await ctx.SubCategories.AddAsync(subCat);
            await ctx.SaveChangesAsync();

            var result = await repo.HasSubCategoriesAsync(1);

            Assert.True(result);
        }

        [Fact]
        public async Task HasSubCategoriesAsync_WithNoSubCategories_ReturnsFalse()
        {
            var ctx = CreateInMemoryContext(nameof(HasSubCategoriesAsync_WithNoSubCategories_ReturnsFalse));
            var repo = new MainCategoryRepository(ctx);

            var cat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Test", MainCategoryPrompt = "P" };
            await ctx.MainCategories.AddAsync(cat);
            await ctx.SaveChangesAsync();

            var result = await repo.HasSubCategoriesAsync(1);

            Assert.False(result);
        }

        #endregion
    }
}
