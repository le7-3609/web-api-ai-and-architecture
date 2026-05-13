using BCrypt.Net;
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
            var hash = BCrypt.Net.BCrypt.HashPassword("pass123");
            var user = new User { Provider = "Local", Email = "test@example.com", Password = hash, FirstName = "John", LastName = "Doe" };

            var result = await _repository.RegisterAsync(user);

            Assert.True(result.UserId > 0);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public async Task GetByEmailForAuthAsync_WithExistingEmail_ReturnsUser()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("secret");
            var user = new User { Provider = "Local", Email = "login@test.com", Password = hash, FirstName = "A", LastName = "B" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByEmailForAuthAsync("login@test.com");

            Assert.NotNull(result);
            Assert.Equal("login@test.com", result.Email);
            Assert.True(BCrypt.Net.BCrypt.Verify("secret", result.Password));
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsUser()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("pass");
            var user = new User { Provider = "Local", Email = "user@test.com", Password = hash, FirstName = "X", LastName = "Y" };
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
                new User { Provider = "Local", Email = "user1@test.com", Password = BCrypt.Net.BCrypt.HashPassword("1"), FirstName = "A", LastName = "B" },
                new User { Provider = "Local", Email = "user2@test.com", Password = BCrypt.Net.BCrypt.HashPassword("2"), FirstName = "C", LastName = "D" }
            );
            _context.SaveChanges();

            var result = await _repository.GetAllAsync();

            Assert.NotNull(result);
            Assert.True(result.Count() >= 2);
        }

        [Fact]
        public async Task UpdateAsync_WithValidUser_UpdatesUser()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("old");
            var user = new User { Provider = "Local", Email = "update@test.com", Password = hash, FirstName = "Old", LastName = "Name" };
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
            var user = new User { Provider = "Local", Email = "orders@test.com", Password = BCrypt.Net.BCrypt.HashPassword("p"), FirstName = "O", LastName = "User" };
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
        public async Task GetByEmailForAuthAsync_WrongEmail_ReturnsNull()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword("correctpass");
            var user = new User { Provider = "Local", Email = "wrong@test.com", Password = hash, FirstName = "A", LastName = "B" };
            _context.Users.Add(user);
            _context.SaveChanges();

            var result = await _repository.GetByEmailForAuthAsync("nobody@test.com");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByEmailForAuthAsync_NonExistentEmail_ReturnsNull()
        {
            var result = await _repository.GetByEmailForAuthAsync("nonexistent@test.com");

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
            var user = new User { Provider = "Local", UserId = 1, Email = "unique@test.com", Password = BCrypt.Net.BCrypt.HashPassword("p"), FirstName = "A", LastName = "B" };
            _context.Users.Add(user);
            _context.SaveChanges();

            var result = await _repository.GetByEmailAsync("unique@test.com", 999);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetAllOrdersAsync_NoOrders_ReturnsEmpty()
        {
            var user = new User { Provider = "Local", Email = "norders@test.com", Password = BCrypt.Net.BCrypt.HashPassword("p"), FirstName = "N", LastName = "O" };
            _context.Users.Add(user);
            _context.SaveChanges();

            var result = await _repository.GetAllOrdersAsync((int)user.UserId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion
    }
}