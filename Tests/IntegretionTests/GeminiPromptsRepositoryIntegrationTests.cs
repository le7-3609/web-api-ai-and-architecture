using Entities;
using Repositories;
using Xunit;

namespace Tests.IntegrationTests
{
    [Collection("Database collection")]
    public class GeminiPromptsRepositoryIntegrationTests
    {
        private readonly DatabaseFixture _fixture;
        private readonly MyShopContext _context;
        private readonly GeminiPromptsRepository _repository;

        public GeminiPromptsRepositoryIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Context;
            _repository = new GeminiPromptsRepository(_context);
            _fixture.ClearDatabase();
        }

        [Fact]
        public async Task AddPromptAsync_SavesPrompt()
        {
            // Seed subcategory
            _context.MainCategories.Add(new MainCategory { MainCategoryId = 1, MainCategoryName = "M" });
            _context.SubCategories.Add(new SubCategory { SubCategoryId = 2, SubCategoryName = "SC2", MainCategoryId = 1 });
            _context.SaveChanges();

            var prompt = new GeminiPrompt { Prompt = "p", SubCategoryId = 2 };
            var saved = await _repository.AddPromptAsync(prompt);

            Assert.True(saved.PromptId > 0);
        }

        [Fact]
        public async Task GetByIDPromptAsync_NonExistent_ReturnsNull()
        {
            var got = await _repository.GetByIDPromptAsync(9999);
            Assert.Null(got);
        }
    }
}
