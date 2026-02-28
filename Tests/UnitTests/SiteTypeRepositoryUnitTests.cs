using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using Xunit;

namespace Tests.UnitTests
{
    public class SiteTypeRepositoryUnitTests
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
        public async Task GetAllAsync_ReturnsAllSiteTypes()
        {
            var ctx = CreateInMemoryContext(nameof(GetAllAsync_ReturnsAllSiteTypes));
            var repo = new SiteTypeRepository(ctx);

            await ctx.SiteTypes.AddRangeAsync(
                new SiteType { SiteTypeId = 1, SiteTypeName = "Shop", SiteTypeNamePrompt = "P", SiteTypeDescriptionPrompt = "P" },
                new SiteType { SiteTypeId = 2, SiteTypeName = "Blog", SiteTypeNamePrompt = "P", SiteTypeDescriptionPrompt = "P" }
            );
            await ctx.SaveChangesAsync();

            var result = await repo.GetAllAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsSiteType()
        {
            var ctx = CreateInMemoryContext(nameof(GetByIdAsync_WithValidId_ReturnsSiteType));
            var repo = new SiteTypeRepository(ctx);

            var siteType = new SiteType { SiteTypeId = 1, SiteTypeName = "Portfolio", SiteTypeNamePrompt = "P", SiteTypeDescriptionPrompt = "P" };
            await ctx.SiteTypes.AddAsync(siteType);
            await ctx.SaveChangesAsync();

            var result = await repo.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Portfolio", result.SiteTypeName);
        }

        [Fact]
        public async Task GetByNameAsync_WithValidName_ReturnsSiteType()
        {
            var ctx = CreateInMemoryContext(nameof(GetByNameAsync_WithValidName_ReturnsSiteType));
            var repo = new SiteTypeRepository(ctx);

            var siteType = new SiteType { SiteTypeId = 1, SiteTypeName = "eCommerce", SiteTypeNamePrompt = "P", SiteTypeDescriptionPrompt = "P" };
            await ctx.SiteTypes.AddAsync(siteType);
            await ctx.SaveChangesAsync();

            var result = await repo.GetByNameAsync("eCommerce");

            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateByMngAsync_WithValidData_Updates()
        {
            var ctx = CreateInMemoryContext(nameof(UpdateByMngAsync_WithValidData_Updates));
            var repo = new SiteTypeRepository(ctx);

            var siteType = new SiteType { SiteTypeId = 1, SiteTypeName = "Old", SiteTypeNamePrompt = "P", SiteTypeDescriptionPrompt = "P" };
            await ctx.SiteTypes.AddAsync(siteType);
            await ctx.SaveChangesAsync();

            siteType.SiteTypeName = "Updated";
            var result = await repo.UpdateByMngAsync(siteType);

            Assert.Equal("Updated", result.SiteTypeName);
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task GetByIdAsync_NonExistentId_ReturnsNull()
        {
            var ctx = CreateInMemoryContext(nameof(GetByIdAsync_NonExistentId_ReturnsNull));
            var repo = new SiteTypeRepository(ctx);

            var result = await repo.GetByIdAsync(9999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByNameAsync_NonExistentName_ReturnsNull()
        {
            var ctx = CreateInMemoryContext(nameof(GetByNameAsync_NonExistentName_ReturnsNull));
            var repo = new SiteTypeRepository(ctx);

            var result = await repo.GetByNameAsync("NonExistent");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_EmptyDatabase_ReturnsEmpty()
        {
            var ctx = CreateInMemoryContext(nameof(GetAllAsync_EmptyDatabase_ReturnsEmpty));
            var repo = new SiteTypeRepository(ctx);

            var result = await repo.GetAllAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}
