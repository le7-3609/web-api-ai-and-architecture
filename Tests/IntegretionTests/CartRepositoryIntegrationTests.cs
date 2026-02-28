using Entities;
using Repositories;
using Xunit;

namespace Tests.IntegrationTests
{
    [Collection("Database collection")]
    public class CartRepositoryIntegrationTests
    {
        private readonly DatabaseFixture _fixture;
        private readonly MyShopContext _context;
        private readonly CartRepository _repository;

        public CartRepositoryIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Context;
            _repository = new CartRepository(_context);
            _fixture.ClearDatabase();
        }

        [Fact]
        public async Task CreateUserCartAsync_CreatesCart()
        {
            var user = new User { Email = "cartuser@test.com", Password = "p", FirstName = "A", LastName = "B", Provider = "Local" };
            _context.Users.Add(user);

            var site = new BasicSite { SiteName = "S1", SiteTypeId = 1, UserDescreption = "d" };
            _context.SiteTypes.Add(new SiteType { SiteTypeId = 1, SiteTypeName = "t", Price = 1 });
            _context.Platforms.Add(new Platform { PlatformId = 1, PlatformName = "pf" });
            _context.BasicSites.Add(site);
            _context.SaveChanges();

            var cart = new Cart { UserId = user.UserId, BasicSiteId = site.BasicSiteId };
            var created = await _repository.CreateUserCartAsync(cart);

            Assert.True(created.CartId > 0);
        }

        [Fact]
        public async Task AddCartItemAsync_AddsItem()
        {
            // seed minimal dependencies
            var user = new User { Email = "itemuser@test.com", Password = "p", FirstName = "A", LastName = "B", Provider = "Local" };
            _context.Users.Add(user);
            _context.SiteTypes.Add(new SiteType { SiteTypeId = 2, SiteTypeName = "t2", Price = 1 });
            var site = new BasicSite { SiteName = "S2", SiteTypeId = 2, UserDescreption = "d" };
            _context.BasicSites.Add(site);

            var product = new Product { ProductName = "Prod", SubCategoryId = 1, ProductPrompt = "P" };
            _context.SubCategories.Add(new SubCategory { SubCategoryId = 1, SubCategoryName = "SC", MainCategoryId = 1, SubCategoryPrompt = "P" });
            _context.MainCategories.Add(new MainCategory { MainCategoryId = 1, MainCategoryName = "M", MainCategoryPrompt = "P" });
            _context.SaveChanges();

            var cart = new Cart { UserId = user.UserId, BasicSiteId = site.BasicSiteId };
            _context.Carts.Add(cart);
            _context.SaveChanges();

            var item = new CartItem { CartId = cart.CartId, ProductId = product.ProductId, PlatformId = null, PromptId = null };
            // ensure product exists
            _context.Products.Add(product);
            _context.SaveChanges();

            var added = await _repository.AddCartItemAsync(item);

            Assert.True(added.CartItemId > 0);
        }
    }
}
