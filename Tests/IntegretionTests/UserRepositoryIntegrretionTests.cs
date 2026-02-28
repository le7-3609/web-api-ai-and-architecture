using Entities;
using Repositories;
using Xunit;

namespace Tests.IntegrationTests
{
    [Collection("Database collection")]
    public class UserRepositoryIntegrationTests
    {
        private readonly DatabaseFixture _fixture;
        private readonly MyShopContext _context;
        private readonly UserRepository _repository;

        public UserRepositoryIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Context;
            _repository = new UserRepository(_context);
            _fixture.ClearDatabase();
        }

        #region Happy Paths

        [Fact]
        public async Task RegisterAsync_WithValidUser_ReturnsUser()
        {
            var user = new User { Provider = "Local", Email = "test@example.com", Password = "pass123", FirstName = "John", LastName = "Doe" };

            var result = await _repository.RegisterAsync(user);

            Assert.True(result.UserId > 0);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsUser()
        {
            var user = new User { Provider = "Local", Email = "login@test.com", Password = "secret", FirstName = "A", LastName = "B" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var result = await _repository.LoginAsync("login@test.com", "secret");

            Assert.NotNull(result);
            Assert.Equal("login@test.com", result.Email);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsUser()
        {
            var user = new User { Provider = "Local", Email = "user@test.com", Password = "pass", FirstName = "X", LastName = "Y" };
            _context.Users.Add(user);
            _context.SaveChanges();

            var result = await _repository.GetByIdAsync((int)user.UserId);

            Assert.NotNull(result);
            Assert.Equal("user@test.com", result.Email);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            _context.Users.AddRange(
                new User { Provider = "Local", Email = "user1@test.com", Password = "1", FirstName = "A", LastName = "B" },
                new User { Provider = "Local", Email = "user2@test.com", Password = "2", FirstName = "C", LastName = "D" }
            );
            _context.SaveChanges();

            var result = await _repository.GetAllAsync();

            Assert.NotNull(result);
            Assert.True(result.Count() >= 2);
        }

        [Fact]
        public async Task UpdateAsync_WithValidUser_UpdatesUser()
        {
            var user = new User { Provider = "Local", Email = "update@test.com", Password = "old", FirstName = "Old", LastName = "Name" };
            _context.Users.Add(user);
            _context.SaveChanges();

            user.FirstName = "New";
            var result = await _repository.UpdateAsync(user);

            Assert.NotNull(result);
            Assert.Equal("New", result.FirstName);
        }

        [Fact]
        public async Task GetAllOrdersAsync_WithExistingOrders_ReturnsOrders()
        {
            var user = new User { Provider = "Local", Email = "orders@test.com", Password = "p", FirstName = "O", LastName = "User" };
            _context.Users.Add(user);
            _context.Statuses.Add(new Status { StatusId = 1, StatusName = "New" });
            _context.SiteTypes.Add(new SiteType { SiteTypeId = 1, SiteTypeName = "Landing", Price = 10 });
            _context.BasicSites.Add(new BasicSite { BasicSiteId = 1, SiteName = "Site", SiteTypeId = 1, UserDescreption = "desc" });
            _context.SaveChanges();

            var order = new Order { UserId = user.UserId, BasicSiteId = 1, Status = 1, OrderSum = 100 };
            _context.Orders.Add(order);
            _context.SaveChanges();

            var result = await _repository.GetAllOrdersAsync((int)user.UserId);

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsNull()
        {
            var user = new User { Provider = "Local", Email = "wrong@test.com", Password = "correctpass", FirstName = "A", LastName = "B" };
            _context.Users.Add(user);
            _context.SaveChanges();

            var result = await _repository.LoginAsync("wrong@test.com", "wrongpass");

            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_NonExistentEmail_ReturnsNull()
        {
            var result = await _repository.LoginAsync("nonexistent@test.com", "anypass");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(9999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByEmailAsync_WithUniqueEmail_ReturnsUser()
        {
            var user = new User { Provider = "Local", UserId = 1, Email = "unique@test.com", Password = "p", FirstName = "A", LastName = "B" };
            _context.Users.Add(user);
            _context.SaveChanges();

            var result = await _repository.GetByEmailAsync("unique@test.com", 999);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetAllOrdersAsync_NoOrders_ReturnsEmpty()
        {
            var user = new User { Provider = "Local", Email = "norders@test.com", Password = "p", FirstName = "N", LastName = "O" };
            _context.Users.Add(user);
            _context.SaveChanges();

            var result = await _repository.GetAllOrdersAsync((int)user.UserId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}