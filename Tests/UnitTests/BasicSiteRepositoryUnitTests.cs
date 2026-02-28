using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using Xunit;

namespace Tests.UnitTests
{
    public class BasicSiteRepositoryUnitTests
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
        public async Task AddBasicSiteAsync_WithValidSite_ReturnsSite()
        {
            var ctx = CreateInMemoryContext(nameof(AddBasicSiteAsync_WithValidSite_ReturnsSite));
            var repo = new BasicSiteRepository(ctx);

            var siteType = new SiteType { SiteTypeId = 1, SiteTypeName = "Shop", SiteTypeDescription = "D", SiteTypeNamePrompt = "P", SiteTypeDescriptionPrompt = "P", Price = 49 };
            var platform = new Platform { PlatformId = 1, PlatformName = "Web", PlatformPrompt = "P" };
            await ctx.SiteTypes.AddAsync(siteType);
            await ctx.Platforms.AddAsync(platform);
            await ctx.SaveChangesAsync();

            var site = new BasicSite { BasicSiteId = 1, SiteName = "MyStore", SiteTypeId = 1, PlatformId = 1 };
            var result = await repo.AddBasicSiteAsync(site);

            Assert.Equal("MyStore", result.SiteName);
            Assert.NotNull(result.SiteType);
            Assert.NotNull(result.Platform);
            Assert.Equal("Shop", result.SiteType.SiteTypeName);
            Assert.Equal("Web", result.Platform.PlatformName);
        }

        [Fact]
        public async Task GetBasicSiteByIdAsync_WithValidId_ReturnsSite()
        {
            var ctx = CreateInMemoryContext(nameof(GetBasicSiteByIdAsync_WithValidId_ReturnsSite));
            var repo = new BasicSiteRepository(ctx);

            var siteType = new SiteType { SiteTypeId = 1, SiteTypeName = "Shop", SiteTypeDescription = "D", SiteTypeNamePrompt = "P", SiteTypeDescriptionPrompt = "P" };
            var platform = new Platform { PlatformId = 1, PlatformName = "Web", PlatformPrompt = "P" };
            await ctx.SiteTypes.AddAsync(siteType);
            await ctx.Platforms.AddAsync(platform);

            var site = new BasicSite { BasicSiteId = 1, SiteName = "Test", SiteTypeId = 1, PlatformId = 1 };
            await ctx.BasicSites.AddAsync(site);
            await ctx.SaveChangesAsync();

            var result = await repo.GetBasicSiteByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Test", result.SiteName);
        }

        [Fact]
        public async Task UpdateBasicSiteAsync_WithValidData_Updates()
        {
            var ctx = CreateInMemoryContext(nameof(UpdateBasicSiteAsync_WithValidData_Updates));
            var repo = new BasicSiteRepository(ctx);

            var site = new BasicSite { BasicSiteId = 1, SiteName = "Old", SiteTypeId = 1, PlatformId = 1 };
            await ctx.BasicSites.AddAsync(site);
            await ctx.SaveChangesAsync();

            site.SiteName = "Updated";
            await repo.UpdateBasicSiteAsync(1, site);

            var updated = await ctx.BasicSites.FindAsync(1L);
            Assert.NotNull(updated);
            Assert.Equal("Updated", updated.SiteName);
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task GetBasicSiteByIdAsync_NonExistentId_ReturnsNull()
        {
            var ctx = CreateInMemoryContext(nameof(GetBasicSiteByIdAsync_NonExistentId_ReturnsNull));
            var repo = new BasicSiteRepository(ctx);

            var result = await repo.GetBasicSiteByIdAsync(9999);

            Assert.Null(result);
        }

        #endregion
    }
}