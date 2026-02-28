using AutoMapper;
using DTO;
using Entities;
using Moq;
using Repositories;
using Services;

namespace Tests.UnitTests
{
    public class SiteTypeServiceUnitTests
    {
        [Fact]
        public async Task GetAllAsync_WhenRepositoryReturnsData_ReturnsMappedDtos()
        {
            var repository = new Mock<ISiteTypeRepository>();
            var mapper = new Mock<IMapper>();

            var entities = new List<SiteType> { new SiteType { SiteTypeId = 1, SiteTypeName = "Shop" } };
            var dtos = new List<SiteTypeDTO> { new SiteTypeDTO(1, "Shop", 10, "desc") };

            repository.Setup(r => r.GetAllAsync()).ReturnsAsync(entities);
            mapper.Setup(m => m.Map<IEnumerable<SiteTypeDTO>>(entities)).Returns(dtos);

            var service = new SiteTypeService(repository.Object, mapper.Object);

            var result = await service.GetAllAsync();

            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal("Shop", resultList[0].SiteTypeName);
        }

        [Fact]
        public async Task UpdateByMngAsync_WhenNameConflict_ThrowsInvalidOperationException()
        {
            var repository = new Mock<ISiteTypeRepository>();
            var mapper = new Mock<IMapper>();

            repository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new SiteType { SiteTypeId = 3, SiteTypeName = "A" });
            repository.Setup(r => r.GetByNameAsync("Dup")).ReturnsAsync(new SiteType { SiteTypeId = 7, SiteTypeName = "Dup" });

            var service = new SiteTypeService(repository.Object, mapper.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateByMngAsync(3, new SiteTypeDTO(3, "Dup", 10, "desc")));
        }
    }
}
