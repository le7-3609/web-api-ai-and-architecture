using Xunit;
using Entities;
using Repositories;
using Services;
using AutoMapper;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.IntegrationTests
{
    [Collection("Database collection")]
    public class OrderServiceIntegrationTests
    {
        private readonly DatabaseFixture _fixture;
        private readonly MyShopContext _context;
        private readonly OrderService _orderService;
        private readonly IMapper _mapper;

        public OrderServiceIntegrationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.Context;
            _fixture.ClearDatabase();

            var services = new ServiceCollection();
            services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Services.Mapper).Assembly));
            var serviceProvider = services.BuildServiceProvider();
            _mapper = serviceProvider.GetRequiredService<IMapper>();

            var orderRepo = new OrderRepository(_context);
            var productRepo = new ProductRepository(_context);
            var cartRepo = new CartRepository(_context);
            var basicSiteServiceMock = new Mock<IBasicSiteService>();
            basicSiteServiceMock.Setup(x => x.GetBasicSitePriceAsync(It.IsAny<long>())).ReturnsAsync(100.0);
            var cartService = new CartService(cartRepo, basicSiteServiceMock.Object, _mapper);
            var loggerMock = new Mock<ILogger<OrderService>>();
            var hostEnvMock = new Mock<IHostEnvironment>();
            var promptBuilderMock = new Mock<IOrderPromptBuilder>();
            promptBuilderMock.Setup(x => x.BuildPromptAsync(It.IsAny<long>(), It.IsAny<ICollection<OrderItem>>()))
                .ReturnsAsync("test prompt");

            _orderService = new OrderService(orderRepo, _mapper, cartService, loggerMock.Object, productRepo, hostEnvMock.Object, promptBuilderMock.Object);
        }

        [Fact]
        public async Task AddOrderFromCartAsync_WithValidCart_CreatesOrder()
        {
            // Arrange
            var user = new User { UserId = 1, Email = "test@test.com", Password = "pass", FirstName = "Test", LastName = "User", Phone = "0500000000", Provider = "Local", ProviderId = "1" };
            _context.Users.Add(user);

            var siteType = new SiteType { SiteTypeId = 1, SiteTypeName = "Blog", Price = 100 };
            _context.SiteTypes.Add(siteType);

            var basicSite = new BasicSite { BasicSiteId = 1, SiteName = "MySite", SiteTypeId = 1, UserDescreption = "desc" };
            _context.BasicSites.Add(basicSite);

            var mainCat = new MainCategory { MainCategoryId = 1, MainCategoryName = "Main", MainCategoryPrompt = "P" };
            _context.MainCategories.Add(mainCat);

            var subCat = new SubCategory { SubCategoryId = 1, MainCategoryId = 1, SubCategoryName = "Sub", SubCategoryPrompt = "P" };
            _context.SubCategories.Add(subCat);

            var product = new Product { ProductId = 1, SubCategoryId = 1, ProductName = "Product", ProductPrompt = "P", Price = 50 };
            _context.Products.Add(product);

            var platform = new Platform { PlatformId = 1, PlatformName = "Platform" };
            _context.Platforms.Add(platform);

            var cart = new Cart { CartId = 1, UserId = 1, BasicSiteId = 1 };
            _context.Carts.Add(cart);

            var cartItem = new CartItem { CartItemId = 1, CartId = 1, ProductId = 1, PlatformId = 1 };
            _context.CartItems.Add(cartItem);

            await _context.SaveChangesAsync();

            // Act
            var result = await _orderService.AddOrderFromCartAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(150, result.OrderSum);
            Assert.Equal(1, result.UserId);
        }
    }
}
