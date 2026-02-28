using AutoMapper;
using DTO;
using Entities;
using Moq;
using Repositories;
using Services;

namespace Tests.UnitTests
{
    public class BasicSiteServiceUnitTests
    {
        [Fact]
        public async Task AddBasicSiteAsync_WithValidDto_ReturnsMappedDto()
        {
            var mapper = new Mock<IMapper>();
            var repository = new Mock<IBasicSiteRepository>();

            var dto = new AddBasicSiteDTO("My Site", "desc", 1, 2);
            var entity = new BasicSite { SiteName = "My Site", UserDescreption = "desc", SiteTypeId = 1, PlatformId = 2 };
            var saved = new BasicSite { BasicSiteId = 10, SiteName = "My Site", UserDescreption = "desc", SiteTypeId = 1, PlatformId = 2 };
            var mappedDto = new BasicSiteDTO { BasicSiteId = 10, SiteName = "My Site", UserDescreption = "desc" };

            mapper.Setup(m => m.Map<BasicSite>(dto)).Returns(entity);
            repository.Setup(r => r.AddBasicSiteAsync(entity)).ReturnsAsync(saved);
            mapper.Setup(m => m.Map<BasicSiteDTO>(saved)).Returns(mappedDto);

            var service = new BasicSiteService(mapper.Object, repository.Object);

            var result = await service.AddBasicSiteAsync(dto);

            Assert.Equal(10, result.BasicSiteId);
            repository.Verify(r => r.AddBasicSiteAsync(entity), Times.Once);
        }

        [Fact]
        public async Task GetBasicSiteByIdAsync_WhenRepositoryReturnsNull_ReturnsNull()
        {
            var mapper = new Mock<IMapper>();
            var repository = new Mock<IBasicSiteRepository>();

            repository.Setup(r => r.GetBasicSiteByIdAsync(99)).ReturnsAsync((BasicSite)null!);
            mapper.Setup(m => m.Map<BasicSiteDTO>(It.IsAny<BasicSite>())).Returns((BasicSiteDTO)null!);

            var service = new BasicSiteService(mapper.Object, repository.Object);

            var result = await service.GetBasicSiteByIdAsync(99);

            Assert.Null(result);
        }
    }
}
