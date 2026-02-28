using AutoMapper;
using DTO;
using Entities;
using Moq;
using Repositories;
using Services;

namespace Tests.UnitTests
{
    public class MainCategoryServiceUnitTests
    {
        [Fact]
        public async Task AddMainCategoryAsync_WithEmptyPrompt_ThrowsArgumentException()
        {
            var repository = new Mock<IMainCategoryRepository>();
            var mapper = new Mock<IMapper>();
            var service = new MainCategoryService(repository.Object, mapper.Object);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AddMainCategoryAsync(new AddMainCategoryDTO("Business", " ")));
        }

        [Fact]
        public async Task UpdateMainCategoryAsync_WhenExistsAndPromptMissing_PreservesExistingPrompt()
        {
            var repository = new Mock<IMainCategoryRepository>();
            var mapper = new Mock<IMapper>();

            repository.Setup(r => r.GetMainCategoryByIdAsync(5)).ReturnsAsync(new MainCategory
            {
                MainCategoryId = 5,
                MainCategoryName = "Old",
                MainCategoryPrompt = "keep-me"
            });

            mapper.Setup(m => m.Map<MainCategory>(It.IsAny<AddMainCategoryDTO>())).Returns(new MainCategory
            {
                MainCategoryName = "New"
            });

            var service = new MainCategoryService(repository.Object, mapper.Object);

            var result = await service.UpdateMainCategoryAsync(5, new AddMainCategoryDTO("New", ""));

            Assert.True(result);
            repository.Verify(r => r.UpdateMainCategoryAsync(It.Is<MainCategory>(x =>
                x.MainCategoryId == 5 && x.MainCategoryPrompt == "keep-me")), Times.Once);
        }

        [Fact]
        public async Task DeleteMainCategoryAsync_WhenNotFound_ReturnsFalse()
        {
            var repository = new Mock<IMainCategoryRepository>();
            var mapper = new Mock<IMapper>();
            repository.Setup(r => r.GetMainCategoryByIdAsync(99)).ReturnsAsync((MainCategory)null!);

            var service = new MainCategoryService(repository.Object, mapper.Object);

            var result = await service.DeleteMainCategoryAsync(99);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteMainCategoryAsync_WithSubCategories_ThrowsInvalidOperationException()
        {
            var repository = new Mock<IMainCategoryRepository>();
            var mapper = new Mock<IMapper>();
            repository.Setup(r => r.GetMainCategoryByIdAsync(1)).ReturnsAsync(new MainCategory { MainCategoryId = 1, MainCategoryName = "Test" });
            repository.Setup(r => r.HasSubCategoriesAsync(1)).ReturnsAsync(true);

            var service = new MainCategoryService(repository.Object, mapper.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteMainCategoryAsync(1));
            Assert.Contains("subcategories", ex.Message, StringComparison.OrdinalIgnoreCase);
            repository.Verify(r => r.DeleteMainCategoryAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteMainCategoryAsync_WithNoSubCategories_ReturnsTrue()
        {
            var repository = new Mock<IMainCategoryRepository>();
            var mapper = new Mock<IMapper>();
            repository.Setup(r => r.GetMainCategoryByIdAsync(1)).ReturnsAsync(new MainCategory { MainCategoryId = 1, MainCategoryName = "Test" });
            repository.Setup(r => r.HasSubCategoriesAsync(1)).ReturnsAsync(false);
            repository.Setup(r => r.DeleteMainCategoryAsync(1)).ReturnsAsync(true);

            var service = new MainCategoryService(repository.Object, mapper.Object);

            var result = await service.DeleteMainCategoryAsync(1);

            Assert.True(result);
            repository.Verify(r => r.DeleteMainCategoryAsync(1), Times.Once);
        }
    }
}
