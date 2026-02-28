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
    public class StatusServiceUnitTests
    {
        [Fact]
        public async Task GetStatusByIdAsync_WhenNotFound_ReturnsNull()
        {
            var mockRepo = new Mock<IStatusRepository>();
            var mockMapper = new Mock<IMapper>();
            mockRepo.Setup(r => r.GetStatusByIdAsync(99)).ReturnsAsync((Status?)null);

            var svc = new StatusService(mockRepo.Object, mockMapper.Object);
            var result = await svc.GetStatusByIdAsync(99);

            Assert.Null(result);
        }
    }
}
