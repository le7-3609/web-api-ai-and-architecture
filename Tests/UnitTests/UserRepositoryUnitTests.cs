using Moq;
using Moq.EntityFrameworkCore;
using Repositories;
using Entities;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.UnitTests
{
    public class UserRepositoryUnitTesting
    {
        #region Happy Paths

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            // Arrange
            var users = new List<User> { new User { UserId = 1 }, new User { UserId = 2 } };
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Users).ReturnsDbSet(users);
            var repo = new UserRepository(mockContext.Object);

            // Act
            var result = await repo.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUser()
        {
            // Arrange
            var users = new List<User> { new User { UserId = 1, Email = "test@test.com" } };
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Users).ReturnsDbSet(users);
            var repo = new UserRepository(mockContext.Object);

            // Act
            var result = await repo.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
        }

        [Fact]
        public async Task GetByEmailAsync_SameUserCheckingSelf_ReturnsNull()
        {
            // Arrange
            var users = new List<User> { new User { UserId = 1, Email = "test@test.com" } };
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Users).ReturnsDbSet(users);
            var repo = new UserRepository(mockContext.Object);

            // Act
            var result = await repo.GetByEmailAsync("test@test.com", 1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterAsync_ValidUser_ReturnsAddedUser()
        {
            // Arrange
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Users).ReturnsDbSet(new List<User>());
            var repo = new UserRepository(mockContext.Object);
            var newUser = new User { Email = "new@test.com", FirstName = "A", LastName = "B", Password = "123" };

            // Act
            var result = await repo.RegisterAsync(newUser);

            // Assert
            Assert.Equal("new@test.com", result.Email);
            mockContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_CorrectCredentials_ReturnsUser()
        {
            // Arrange
            var users = new List<User> { new User { Email = "u@u.com", Password = "123" } };
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Users).ReturnsDbSet(users);
            var repo = new UserRepository(mockContext.Object);

            // Act
            var result = await repo.LoginAsync("u@u.com", "123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("u@u.com", result.Email);
        }

        [Fact]
        public async Task UpdateAsync_ValidUpdate_ReturnsUpdatedUser()
        {
            // Arrange
            var user = new User { UserId = 1, FirstName = "OldName", Email = "u@u.com" };
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Users).ReturnsDbSet(new List<User> { user });
            var repo = new UserRepository(mockContext.Object);

            // Act
            user.FirstName = "NewName";
            var result = await repo.UpdateAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("NewName", result.FirstName);
            mockContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task GetAllOrdersAsync_ReturnsOnlySpecificUserOrders()
        {
            // Arrange
            var orders = new List<Order> {
                new Order { OrderId = 1, UserId = 10 },
                new Order { OrderId = 2, UserId = 20 }
            };
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Orders).ReturnsDbSet(orders);
            var repo = new UserRepository(mockContext.Object);

            // Act
            var result = await repo.GetAllOrdersAsync(10);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(10, result.First().UserId);
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Users).ReturnsDbSet(new List<User>());
            var repo = new UserRepository(mockContext.Object);

            // Act
            var result = await repo.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByEmailAsync_EmailTakenByAnother_ReturnsUser()
        {
            // Arrange
            var users = new List<User> { new User { UserId = 1, Email = "taken@test.com" } };
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Users).ReturnsDbSet(users);
            var repo = new UserRepository(mockContext.Object);

            // Act
            var result = await repo.GetByEmailAsync("taken@test.com", 2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("taken@test.com", result.Email);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ReturnsNull()
        {
            // Arrange
            var users = new List<User> { new User { Email = "u@u.com", Password = "123" } };
            var mockContext = new Mock<MyShopContext>();
            mockContext.Setup(x => x.Users).ReturnsDbSet(users);
            var repo = new UserRepository(mockContext.Object);

            // Act
            var result = await repo.LoginAsync("u@u.com", "wrong_pass");

            // Assert
            Assert.Null(result);
        }

        #endregion
    }
}

