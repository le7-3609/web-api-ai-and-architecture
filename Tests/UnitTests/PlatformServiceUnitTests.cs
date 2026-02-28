using AutoMapper;
using DTO;
using Entities;
using Moq;
using Repositories;
using Services;

namespace Tests.UnitTests
{
    public class PlatformServiceUnitTests
    {
        [Fact]
        public async Task AddPlatformAsync_WhenNameExists_ReturnsNull()
        {
            var repository = new Mock<IPlatformRepository>();
            var mapper = new Mock<IMapper>();
            repository.Setup(r => r.GetPlatformByNameAsync("Webflow")).ReturnsAsync(new Platform { PlatformId = 1, PlatformName = "Webflow" });

            var service = new PlatformService(repository.Object, mapper.Object);

            var result = await service.AddPlatformAsync("Webflow");

            Assert.Null(result);
            repository.Verify(r => r.AddPlatformAsync(It.IsAny<Platform>()), Times.Never);
        }

        [Fact]
        public async Task AddPlatformAsync_WhenNewName_ReturnsMappedDto()
        {
            var repository = new Mock<IPlatformRepository>();
            var mapper = new Mock<IMapper>();

            repository.Setup(r => r.GetPlatformByNameAsync("Framer")).ReturnsAsync((Platform)null!);
            var saved = new Platform { PlatformId = 11, PlatformName = "Framer" };
            repository.Setup(r => r.AddPlatformAsync(It.IsAny<Platform>())).ReturnsAsync(saved);
            mapper.Setup(m => m.Map<PlatformsDTO>(saved)).Returns(new PlatformsDTO(11, "Framer", ""));

            var service = new PlatformService(repository.Object, mapper.Object);

            var result = await service.AddPlatformAsync("Framer");

            Assert.NotNull(result);
            Assert.Equal(11, result.PlatformId);
        }

        [Fact]
        public async Task DeletePlatformAsync_DefaultPlatformId1_ThrowsInvalidOperationException()
        {
            var repository = new Mock<IPlatformRepository>();
            var mapper = new Mock<IMapper>();

            var service = new PlatformService(repository.Object, mapper.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeletePlatformAsync(1));
            Assert.Contains("default", ex.Message, StringComparison.OrdinalIgnoreCase);
            repository.Verify(r => r.DeletePlatformAsync(It.IsAny<int>()), Times.Never);
            repository.Verify(r => r.ReassignPlatformReferencesAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeletePlatformAsync_WhenNotFound_ReturnsFalse()
        {
            var repository = new Mock<IPlatformRepository>();
            var mapper = new Mock<IMapper>();
            repository.Setup(r => r.GetPlatformByIdAsync(99)).ReturnsAsync((Platform)null!);

            var service = new PlatformService(repository.Object, mapper.Object);

            var result = await service.DeletePlatformAsync(99);

            Assert.False(result);
        }

        [Fact]
        public async Task DeletePlatformAsync_ValidPlatform_ReassignsAndDeletes()
        {
            var repository = new Mock<IPlatformRepository>();
            var mapper = new Mock<IMapper>();
            repository.Setup(r => r.GetPlatformByIdAsync(5)).ReturnsAsync(new Platform { PlatformId = 5, PlatformName = "Old" });
            repository.Setup(r => r.ReassignPlatformReferencesAsync(5, 1)).Returns(Task.CompletedTask);
            repository.Setup(r => r.DeletePlatformAsync(5)).ReturnsAsync(true);

            var service = new PlatformService(repository.Object, mapper.Object);

            var result = await service.DeletePlatformAsync(5);

            Assert.True(result);
            repository.Verify(r => r.ReassignPlatformReferencesAsync(5, 1), Times.Once);
            repository.Verify(r => r.DeletePlatformAsync(5), Times.Once);
        }
    }
}
