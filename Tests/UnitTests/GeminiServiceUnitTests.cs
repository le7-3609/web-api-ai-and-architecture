using System.Threading.Tasks;
using AutoMapper;
using DTO;
using Entities;
using Moq;
using Repositories;
using Services;
using Xunit;

namespace Tests.UnitTests
{
    public class GeminiServiceUnitTests
    {
        [Fact]
        public async Task AddGeminiForUserProductAsync_SavesPromptAndReturnsSuccess()
        {
            var mockGemini = new Mock<IGemini>();
            var mockSubCat = new Mock<ISubCategoryRepository>();
            var mockMainCat = new Mock<IMainCategoryRepository>();
            var mockPromptsRepo = new Mock<IGeminiPromptsRepository>();
            var mockMapper = new Mock<IMapper>();

            mockSubCat.Setup(s => s.GetSubCategoryByIdAsync(10)).ReturnsAsync(new SubCategory
            {
                SubCategoryId = 10L,
                SubCategoryName = "Widgets",
                SubCategoryPrompt = "Build modern widget pages"
            });
            mockGemini.Setup(g => g.RunGeminiForUserProduct(
                "User Request: please make me a widget\nSub Category Name: Widgets\nSub Category Prompt: Build modern widget pages",
                "Widgets")).ReturnsAsync("generated-prompt");
            var saved = new GeminiPrompt { PromptId = 123, Prompt = "generated-prompt", SubCategoryId = 10L };
            mockPromptsRepo.Setup(r => r.AddPromptAsync(It.IsAny<GeminiPrompt>())).ReturnsAsync(saved);
            mockMapper.Setup(m => m.Map<GeminiPromptDTO>(It.IsAny<GeminiPrompt>()))
                .Returns((GeminiPrompt p) => new GeminiPromptDTO(p.PromptId, p.Prompt, p.SubCategoryId));

            var svc = new GeminiService(mockGemini.Object, mockSubCat.Object, mockMainCat.Object, mockPromptsRepo.Object, mockMapper.Object);

            var res = await svc.AddGeminiForUserProductAsync(10, "please make me a widget");

            Assert.NotNull(res);
            Assert.Equal(saved.PromptId, res.PromptId);
            mockPromptsRepo.Verify(r => r.AddPromptAsync(It.IsAny<GeminiPrompt>()), Times.Once);
        }

        [Fact]
        public async Task AddGeminiForUserProductAsync_WithoutSubCategoryId_UsesOriginalRequest()
        {
            var mockGemini = new Mock<IGemini>();
            var mockSubCat = new Mock<ISubCategoryRepository>();
            var mockMainCat = new Mock<IMainCategoryRepository>();
            var mockPromptsRepo = new Mock<IGeminiPromptsRepository>();
            var mockMapper = new Mock<IMapper>();

            mockGemini.Setup(g => g.RunGeminiForUserProduct("plain-request", "General")).ReturnsAsync("generated-prompt");
            var saved = new GeminiPrompt { PromptId = 124, Prompt = "generated-prompt", SubCategoryId = null };
            mockPromptsRepo.Setup(r => r.AddPromptAsync(It.IsAny<GeminiPrompt>())).ReturnsAsync(saved);
            mockMapper.Setup(m => m.Map<GeminiPromptDTO>(It.IsAny<GeminiPrompt>()))
                .Returns((GeminiPrompt p) => new GeminiPromptDTO(p.PromptId, p.Prompt, p.SubCategoryId));

            var svc = new GeminiService(mockGemini.Object, mockSubCat.Object, mockMainCat.Object, mockPromptsRepo.Object, mockMapper.Object);

            var res = await svc.AddGeminiForUserProductAsync(null, "plain-request");

            Assert.NotNull(res);
            Assert.Equal(saved.PromptId, res.PromptId);
            mockSubCat.Verify(s => s.GetSubCategoryByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task UpdateGeminiForUserProductAsync_UpdatesPrompt_WhenExists()
        {
            var mockGemini = new Mock<IGemini>();
            var mockSubCat = new Mock<ISubCategoryRepository>();
            var mockMainCat = new Mock<IMainCategoryRepository>();
            var mockPromptsRepo = new Mock<IGeminiPromptsRepository>();
            var mockMapper = new Mock<IMapper>();

            var existing = new GeminiPrompt { PromptId = 50, Prompt = "old", SubCategoryId = 7L };
            mockPromptsRepo.Setup(r => r.GetByIDPromptAsync(50)).ReturnsAsync(existing);
            mockSubCat.Setup(s => s.GetSubCategoryByIdAsync(7)).ReturnsAsync(new SubCategory
            {
                SubCategoryId = 7L,
                SubCategoryName = "Gadgets",
                SubCategoryPrompt = "Technical gadget specification"
            });
            mockGemini.Setup(g => g.RunGeminiForUserProduct(
                "User Request: revise\nSub Category Name: Gadgets\nSub Category Prompt: Technical gadget specification",
                "Gadgets")).ReturnsAsync("new-prompt");
            mockMapper.Setup(m => m.Map<GeminiPromptDTO>(It.IsAny<GeminiPrompt>()))
                .Returns((GeminiPrompt p) => new GeminiPromptDTO(p.PromptId, p.Prompt, p.SubCategoryId));

            var svc = new GeminiService(mockGemini.Object, mockSubCat.Object, mockMainCat.Object, mockPromptsRepo.Object, mockMapper.Object);

            var res = await svc.UpdateGeminiForUserProductAsync(50, "revise");

            Assert.NotNull(res);
            Assert.Equal(existing.PromptId, res.PromptId);
            mockPromptsRepo.Verify(r => r.UpdatePromptAsync(50, It.Is<GeminiPrompt>(p => p.Prompt == "new-prompt")), Times.Once);
        }

        [Fact]
        public async Task GetByIdPromptAsync_DelegatesToRepository()
        {
            var mockGemini = new Mock<IGemini>();
            var mockSubCat = new Mock<ISubCategoryRepository>();
            var mockMainCat = new Mock<IMainCategoryRepository>();
            var mockPromptsRepo = new Mock<IGeminiPromptsRepository>();
            var mockMapper = new Mock<IMapper>();

            var existing = new GeminiPrompt { PromptId = 900 };
            mockPromptsRepo.Setup(r => r.GetByIDPromptAsync(900)).ReturnsAsync(existing);
            mockMapper.Setup(m => m.Map<GeminiPromptDTO>(It.IsAny<GeminiPrompt>()))
                .Returns((GeminiPrompt p) => new GeminiPromptDTO(p.PromptId, p.Prompt, p.SubCategoryId));

            var svc = new GeminiService(mockGemini.Object, mockSubCat.Object, mockMainCat.Object, mockPromptsRepo.Object, mockMapper.Object);
            var got = await svc.GetByIdPromptAsync(900);
            Assert.NotNull(got);
            Assert.Equal(existing.PromptId, got.PromptId);
        }
    }
}
