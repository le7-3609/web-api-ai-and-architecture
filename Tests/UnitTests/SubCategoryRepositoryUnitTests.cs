using Repositories;
using Entities;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tests.IntegrationTests;

namespace Tests.UnitTests
{
    public class SubCategoryRepositoryUnitTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly MyShopContext _context;

        public SubCategoryRepositoryUnitTests()
        {
            // Create SQLite in-memory database for unit tests
            _connection = new SqliteConnection("DataSource=:memory:;");
            _connection.Open();

            var options = new DbContextOptionsBuilder<MyShopContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new TestMyShopContext(options);
            _context.Database.EnsureCreatedAsync().Wait();
        }

        #region Happy Paths

        [Fact]
        public async Task GetSubCategoryByIdAsync_ExistingId_ReturnsSubCategory()
        {
            // Arrange
            var mainCat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Main", MainCategoryPrompt = "P" };
            var subCat = new SubCategory { SubCategoryId = 1, SubCategoryName = "Laptops", MainCategoryId = 1, SubCategoryPrompt = "P" };
            _context.MainCategories.Add(mainCat);
            _context.SubCategories.Add(subCat);
            await _context.SaveChangesAsync();

            var repo = new SubCategoryRepository(_context);

            // Act
            var result = await repo.GetSubCategoryByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Laptops", result.SubCategoryName);
        }

        [Fact]
        public async Task GetSubCategoryAsync_FilterByDescription_ReturnsMatchingItems()
        {
            // Arrange
            var mainCat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Main", MainCategoryPrompt = "P" };
            _context.MainCategories.Add(mainCat);
            
            var subCategories = new List<SubCategory>
            {
                new SubCategory { SubCategoryName = "S1", CategoryDescription = "Apple Device", MainCategoryId = 1, SubCategoryPrompt = "P" },
                new SubCategory { SubCategoryName = "S2", CategoryDescription = "Samsung Device", MainCategoryId = 1, SubCategoryPrompt = "P" }
            };
            _context.SubCategories.AddRange(subCategories);
            await _context.SaveChangesAsync();

            var repo = new SubCategoryRepository(_context);

            // Act
            var (results, total) = await repo.GetSubCategoryAsync(10, 0, "Apple", new int?[] { 1 });

            // Assert
            Assert.Equal(1, total);
            Assert.Equal("S1", results.First().SubCategoryName);
        }

        [Fact]
        public async Task AddSubCategoryAsync_ValidSubCategory_SavesAndReturns()
        {
            // Arrange
            var mainCat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Main", MainCategoryPrompt = "P" };
            _context.MainCategories.Add(mainCat);
            await _context.SaveChangesAsync();
            
            var repo = new SubCategoryRepository(_context);
            var newSub = new SubCategory { SubCategoryName = "Hardware", MainCategoryId = 1, SubCategoryPrompt = "P" };

            // Act
            var result = await repo.AddSubCategoryAsync(newSub);

            // Assert
            Assert.Equal("Hardware", result.SubCategoryName);
            Assert.True(result.SubCategoryId > 0);
        }

        [Fact]
        public async Task UpdateSubCategoryAsync_ValidData_UpdatesSuccessfully()
        {
            // Arrange
            var mainCat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Main", MainCategoryPrompt = "P" };
            _context.MainCategories.Add(mainCat);
            
            var sub = new SubCategory { SubCategoryId = 1, SubCategoryName = "Old Name", MainCategoryId = 1, SubCategoryPrompt = "P" };
            _context.SubCategories.Add(sub);
            await _context.SaveChangesAsync();

            var repo = new SubCategoryRepository(_context);
            
            // Fetch the tracked entity and update it
            var subToUpdate = await _context.SubCategories.FirstOrDefaultAsync(x => x.SubCategoryId == 1);
            Assert.NotNull(subToUpdate);
            subToUpdate.SubCategoryName = "Updated Name";

            // Act
            await repo.UpdateSubCategoryAsync(1, subToUpdate);

            // Assert
            var updated = await _context.SubCategories.FirstOrDefaultAsync(x => x.SubCategoryId == 1);
            Assert.NotNull(updated);
            Assert.Equal("Updated Name", updated.SubCategoryName);
        }

        [Fact]
        public async Task DeleteSubCategoryAsync_NoLinkedProducts_ReturnsTrue()
        {
            // Arrange
            var mainCat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Main", MainCategoryPrompt = "P" };
            _context.MainCategories.Add(mainCat);
            
            var sub = new SubCategory { SubCategoryId = 1, SubCategoryName = "ToDelete", MainCategoryId = 1, SubCategoryPrompt = "P" };
            _context.SubCategories.Add(sub);
            await _context.SaveChangesAsync();

            var repo = new SubCategoryRepository(_context);

            // Act
            var result = await repo.DeleteSubCategoryAsync(1);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task DeleteSubCategoryAsync_NonExistingId_ReturnsFalse()
        {
            // Arrange
            var repo = new SubCategoryRepository(_context);

            // Act
            var result = await repo.DeleteSubCategoryAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasProductsAsync_WithLinkedProducts_ReturnsTrue()
        {
            // Arrange
            var mainCat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Main", MainCategoryPrompt = "P" };
            _context.MainCategories.Add(mainCat);

            var subId = 1;
            var sub = new SubCategory { SubCategoryId = subId, SubCategoryName = "Test", MainCategoryId = 1, SubCategoryPrompt = "P" };
            _context.SubCategories.Add(sub);
            await _context.SaveChangesAsync();

            var product = new Product { ProductId = 10, SubCategoryId = subId, ProductName = "P", ProductPrompt = "P" };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var repo = new SubCategoryRepository(_context);

            // Act
            var result = await repo.HasProductsAsync(subId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasProductsAsync_WithNoProducts_ReturnsFalse()
        {
            // Arrange
            var mainCat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Main", MainCategoryPrompt = "P" };
            _context.MainCategories.Add(mainCat);

            var sub = new SubCategory { SubCategoryId = 1, SubCategoryName = "Empty", MainCategoryId = 1, SubCategoryPrompt = "P" };
            _context.SubCategories.Add(sub);
            await _context.SaveChangesAsync();

            var repo = new SubCategoryRepository(_context);

            // Act
            var result = await repo.HasProductsAsync(1);

            // Assert
            Assert.False(result);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}