using AutoMapper;
using DTO;
using Entities;
using Moq;
using Repositories;
using Services;

namespace Tests.UnitTests
{
    public class SubCategoryServiceUnitTests
    {
        [Fact]
        public async Task AddSubCategoryAsync_WhenMainCategoryMissing_ThrowsInvalidOperationException()
        {
            var repository = new Mock<ISubCategoryRepository>();
            var mapper = new Mock<IMapper>();

            repository.Setup(r => r.GetByNameAsync("Landing")).ReturnsAsync((SubCategory)null!);
            repository.Setup(r => r.MainCategoryExistsAsync(99)).ReturnsAsync(false);

            var service = new SubCategoryService(repository.Object, mapper.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AddSubCategoryAsync(new AddSubCategoryDTO(99, "Landing", "p", "img", "desc")));
        }

        [Fact]
        public async Task AddSubCategoryAsync_WhenValid_AddsAndReturnsDto()
        {
            var repository = new Mock<ISubCategoryRepository>();
            var mapper = new Mock<IMapper>();

            var dto = new AddSubCategoryDTO(1, "Landing", "p", "img", "desc");
            var mapped = new SubCategory { MainCategoryId = 1, SubCategoryName = "Landing" };
            var saved = new SubCategory { SubCategoryId = 10, MainCategoryId = 1, SubCategoryName = "Landing", SubCategoryPrompt = "gfasdfghfh", ImageUrl = "img", CategoryDescription = "desc" };
            var mappedDto = new SubCategoryDTO(10, 1, "Landing", "gfasdfghfh", "img", "desc");

            repository.Setup(r => r.GetByNameAsync("Landing")).ReturnsAsync((SubCategory)null!);
            repository.Setup(r => r.MainCategoryExistsAsync(1)).ReturnsAsync(true);
            mapper.Setup(m => m.Map<SubCategory>(dto)).Returns(mapped);
            repository.Setup(r => r.AddSubCategoryAsync(It.IsAny<SubCategory>())).ReturnsAsync(saved);
            mapper.Setup(m => m.Map<SubCategoryDTO>(saved)).Returns(mappedDto);

            var service = new SubCategoryService(repository.Object, mapper.Object);

            var result = await service.AddSubCategoryAsync(dto);

            Assert.Equal(10, result.SubCategoryId);
            repository.Verify(r => r.AddSubCategoryAsync(It.Is<SubCategory>(x => x.SubCategoryPrompt == "gfasdfghfh")), Times.Once);
        }

        [Fact]
        public async Task DeleteSubCategoryAsync_WhenNotFound_ReturnsFalse()
        {
            var repository = new Mock<ISubCategoryRepository>();
            var mapper = new Mock<IMapper>();
            repository.Setup(r => r.GetSubCategoryByIdAsync(99)).ReturnsAsync((SubCategory)null!);

            var service = new SubCategoryService(repository.Object, mapper.Object);

            var result = await service.DeleteSubCategoryAsync(99);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteSubCategoryAsync_WithProducts_ThrowsInvalidOperationException()
        {
            var repository = new Mock<ISubCategoryRepository>();
            var mapper = new Mock<IMapper>();
            repository.Setup(r => r.GetSubCategoryByIdAsync(1)).ReturnsAsync(new SubCategory { SubCategoryId = 1, SubCategoryName = "Test" });
            repository.Setup(r => r.HasProductsAsync(1)).ReturnsAsync(true);

            var service = new SubCategoryService(repository.Object, mapper.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteSubCategoryAsync(1));
            Assert.Contains("products", ex.Message, StringComparison.OrdinalIgnoreCase);
            repository.Verify(r => r.DeleteSubCategoryAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteSubCategoryAsync_WithNoProducts_ReturnsTrue()
        {
            var repository = new Mock<ISubCategoryRepository>();
            var mapper = new Mock<IMapper>();
            repository.Setup(r => r.GetSubCategoryByIdAsync(1)).ReturnsAsync(new SubCategory { SubCategoryId = 1, SubCategoryName = "Test" });
            repository.Setup(r => r.HasProductsAsync(1)).ReturnsAsync(false);
            repository.Setup(r => r.DeleteSubCategoryAsync(1)).ReturnsAsync(true);

            var service = new SubCategoryService(repository.Object, mapper.Object);

            var result = await service.DeleteSubCategoryAsync(1);

            Assert.True(result);
            repository.Verify(r => r.DeleteSubCategoryAsync(1), Times.Once);
        }
    }
}
