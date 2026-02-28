using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using Xunit;

namespace Tests.UnitTests
{
    public class PlatformRepositoryUnitTests
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
        public async Task AddPlatformAsync_WithValidPlatform_ReturnsPlatform()
        {
            var ctx = CreateInMemoryContext(nameof(AddPlatformAsync_WithValidPlatform_ReturnsPlatform));
            var repo = new PlatformRepository(ctx);

            var platform = new Platform { PlatformName = "Web", PlatformPrompt = "P" };
            var result = await repo.AddPlatformAsync(platform);

            Assert.True(result.PlatformId > 0);
        }

        [Fact]
        public async Task GetPlatformByIdAsync_WithValidId_ReturnsPlatform()
        {
            var ctx = CreateInMemoryContext(nameof(GetPlatformByIdAsync_WithValidId_ReturnsPlatform));
            var repo = new PlatformRepository(ctx);

            var platform = new Platform { PlatformId = 1, PlatformName = "Mobile", PlatformPrompt = "P" };
            await ctx.Platforms.AddAsync(platform);
            await ctx.SaveChangesAsync();

            var result = await repo.GetPlatformByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Mobile", result.PlatformName);
        }

        [Fact]
        public async Task GetPlatformsAsync_ReturnsAllPlatforms()
        {
            var ctx = CreateInMemoryContext(nameof(GetPlatformsAsync_ReturnsAllPlatforms));
            var repo = new PlatformRepository(ctx);

            await ctx.Platforms.AddRangeAsync(
                new Platform { PlatformName = "Web", PlatformPrompt = "P" },
                new Platform { PlatformName = "Mobile", PlatformPrompt = "P" }
            );
            await ctx.SaveChangesAsync();

            var result = await repo.GetPlatformsAsync();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task UpdatePlatformAsync_WithValidData_ReturnsTrue()
        {
            var ctx = CreateInMemoryContext(nameof(UpdatePlatformAsync_WithValidData_ReturnsTrue));
            var repo = new PlatformRepository(ctx);

            var platform = new Platform { PlatformId = 1, PlatformName = "Old", PlatformPrompt = "P" };
            await ctx.Platforms.AddAsync(platform);
            await ctx.SaveChangesAsync();

            var updated = new Platform { PlatformId = 1, PlatformName = "New", PlatformPrompt = "P" };
            var result = await repo.UpdatePlatformAsync(1, updated);

            Assert.True(result);
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task GetPlatformByIdAsync_NonExistentId_ReturnsNull()
        {
            var ctx = CreateInMemoryContext(nameof(GetPlatformByIdAsync_NonExistentId_ReturnsNull));
            var repo = new PlatformRepository(ctx);

            var result = await repo.GetPlatformByIdAsync(9999);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdatePlatformAsync_NonExistentId_ReturnsFalse()
        {
            var ctx = CreateInMemoryContext(nameof(UpdatePlatformAsync_NonExistentId_ReturnsFalse));
            var repo = new PlatformRepository(ctx);

            var platform = new Platform { PlatformName = "Test", PlatformPrompt = "P" };
            var result = await repo.UpdatePlatformAsync(9999, platform);

            Assert.False(result);
        }

        [Fact]
        public async Task DeletePlatformAsync_NonExistentId_ReturnsFalse()
        {
            var ctx = CreateInMemoryContext(nameof(DeletePlatformAsync_NonExistentId_ReturnsFalse));
            var repo = new PlatformRepository(ctx);

            var result = await repo.DeletePlatformAsync(9999);

            Assert.False(result);
        }

        #endregion
    }
}
