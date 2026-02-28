using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using Xunit;

namespace Tests.UnitTests
{
    public class GeminiPromptsRepositoryUnitTests
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
        public async Task AddPromptAsync_WithValidPrompt_ReturnsPrompt()
        {
            var ctx = CreateInMemoryContext(nameof(AddPromptAsync_WithValidPrompt_ReturnsPrompt));
            var repo = new GeminiPromptsRepository(ctx);

            var prompt = new GeminiPrompt { PromptId = 1, Prompt = "Test prompt content", SubCategoryId = 1 };
            var result = await repo.AddPromptAsync(prompt);

            Assert.True(result.PromptId > 0);
        }

        [Fact]
        public async Task GetByIDPromptAsync_WithValidId_ReturnsPrompt()
        {
            var ctx = CreateInMemoryContext(nameof(GetByIDPromptAsync_WithValidId_ReturnsPrompt));
            var repo = new GeminiPromptsRepository(ctx);

            var prompt = new GeminiPrompt { PromptId = 1, Prompt = "Content", SubCategoryId = 1 };
            await ctx.GeminiPrompts.AddAsync(prompt);
            await ctx.SaveChangesAsync();

            var result = await repo.GetByIDPromptAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Content", result.Prompt);
        }

        [Fact]
        public async Task UpdatePromptAsync_WithValidData_Updates()
        {
            var ctx = CreateInMemoryContext(nameof(UpdatePromptAsync_WithValidData_Updates));
            var repo = new GeminiPromptsRepository(ctx);

            var prompt = new GeminiPrompt { PromptId = 1, Prompt = "Old", SubCategoryId = 1 };
            await ctx.GeminiPrompts.AddAsync(prompt);
            await ctx.SaveChangesAsync();

            prompt.Prompt = "Updated";
            await repo.UpdatePromptAsync(1, prompt);

            var updated = await ctx.GeminiPrompts.FindAsync(1L);
            Assert.NotNull(updated);
            Assert.Equal("Updated", updated.Prompt);
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task GetByIDPromptAsync_NonExistentId_ReturnsNull()
        {
            var ctx = CreateInMemoryContext(nameof(GetByIDPromptAsync_NonExistentId_ReturnsNull));
            var repo = new GeminiPromptsRepository(ctx);

            var result = await repo.GetByIDPromptAsync(9999);

            Assert.Null(result);
        }

        #endregion
    }
}
