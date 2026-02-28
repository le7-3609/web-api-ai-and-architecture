using Entities;
using Repositories;
using Xunit;

namespace Tests.IntegrationTests
{
    [Collection("Database collection")]
    public class PlatformRepositoryIntegrationTests
    {
        private readonly DatabaseFixture _fixture;
        private readonly MyShopContext _context;
        private readonly PlatformRepository _repository;

        public PlatformRepositoryIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Context;
            _repository = new PlatformRepository(_context);
            _fixture.ClearDatabase();
        }

        [Fact]
        public async Task AddPlatformAsync_AddsPlatform()
        {
            var platform = new Platform { PlatformName = "NewPlat" };
            var saved = await _repository.AddPlatformAsync(platform);
            Assert.True(saved.PlatformId > 0);
        }

        [Fact]
        public async Task GetPlatformByIdAsync_ReturnsPlatform()
        {
            var p = new Platform { PlatformId = 77, PlatformName = "P77" };
            _context.Platforms.Add(p);
            _context.SaveChanges();

            var got = await _repository.GetPlatformByIdAsync(77);
            Assert.NotNull(got);
            Assert.Equal("P77", got.PlatformName);
        }

        [Fact]
        public async Task ReassignPlatformReferencesAsync_ReassignsCartItems()
        {
            var defaultPlatform = new Platform { PlatformId = 1, PlatformName = "Default" };
            var oldPlatform = new Platform { PlatformId = 50, PlatformName = "OldPlat" };
            _context.Platforms.AddRange(defaultPlatform, oldPlatform);

            var user = new User
            {
                UserId = 1,
                Email = "u@u.com",
                Password = "p",
                FirstName = "U",
                LastName = "L",
                Phone = "0500000000",
                Provider = "Local",
                ProviderId = "1"
            };
            _context.Users.Add(user);

            var siteType = new SiteType { SiteTypeId = 1, SiteTypeName = "T" };
            _context.SiteTypes.Add(siteType);

            var basicSite = new BasicSite { BasicSiteId = 1, SiteName = "S", SiteTypeId = 1, PlatformId = 50 };
            _context.BasicSites.Add(basicSite);

            var cart = new Cart { CartId = 1, UserId = 1 };
            _context.Carts.Add(cart);

            var product = new Product { ProductId = 1, ProductName = "P", SubCategoryId = 1, ProductPrompt = "P" };
            _context.Products.Add(product);

            var cartItem = new CartItem { CartItemId = 1, CartId = 1, ProductId = 1, PlatformId = 50 };
            _context.CartItems.Add(cartItem);

            var order = new Order { OrderId = 1, BasicSiteId = 1, UserId = 1, Status = 1 };
            _context.Orders.Add(order);

            var orderItem = new OrderItem { OrderItemId = 1, ProductId = 1, OrderId = 1, PlatformId = 50 };
            _context.OrderItems.Add(orderItem);

            _context.SaveChanges();

            await _repository.ReassignPlatformReferencesAsync(50, 1);

            var updatedCartItem = _context.CartItems.Find(1L);
            var updatedOrderItem = _context.OrderItems.Find(1L);
            var updatedBasicSite = _context.BasicSites.Find(1L);

            Assert.Equal(1, updatedCartItem!.PlatformId);
            Assert.Equal(1, updatedOrderItem!.PlatformId);
            Assert.Equal(1, updatedBasicSite!.PlatformId);
        }

        [Fact]
        public async Task ReassignPlatformReferencesAsync_NoReferences_CompletesWithoutError()
        {
            var defaultPlatform = new Platform { PlatformId = 1, PlatformName = "Default" };
            var otherPlatform = new Platform { PlatformId = 60, PlatformName = "Other" };
            _context.Platforms.AddRange(defaultPlatform, otherPlatform);
            _context.SaveChanges();

            await _repository.ReassignPlatformReferencesAsync(60, 1);

            // Should complete without error
        }
    }
}
