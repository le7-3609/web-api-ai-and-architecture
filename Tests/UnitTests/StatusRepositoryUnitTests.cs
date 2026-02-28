using Moq;
using Moq.EntityFrameworkCore;
using Repositories;
using Entities;
using Xunit;

namespace Tests.UnitTests
{
    public class StatusRepositoryUnitTests
    {
        private readonly Mock<MyShopContext> _mockContext;
        private readonly StatusRepository _repo;

        public StatusRepositoryUnitTests()
        {
            _mockContext = new Mock<MyShopContext>();
            _repo = new StatusRepository(_mockContext.Object);
        }

        #region Happy Paths

        [Fact]
        public async Task GetAllStatusesAsync_ReturnsAll()
        {
            var data = new List<Status>
            {
                new Status { StatusId = 1, StatusName = "Pending" },
                new Status { StatusId = 2, StatusName = "Completed" }
            };
            _mockContext.Setup(x => x.Statuses).ReturnsDbSet(data);

            var result = await _repo.GetAllStatusesAsync();

            Assert.Equal(2, result.Count());
        }
        #endregion
    }
}
