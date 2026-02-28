using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using Xunit;

namespace Tests.UnitTests
{
    public class CartRepositoryUnitTests
    {
        private static MyShopContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<MyShopContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new MyShopContext(options);
        }

        #region Happy Paths

        [Fact]
        public async Task CreateUserCartAsync_WithValidCart_ReturnsCart()
        {
            var ctx = CreateInMemoryContext(nameof(CreateUserCartAsync_WithValidCart_ReturnsCart));
            var repo = new CartRepository(ctx);

            var cart = new Cart { CartId = 1, UserId = 1, BasicSiteId = 1 };
            var result = await repo.CreateUserCartAsync(cart);

            Assert.Equal(cart.CartId, result.CartId);
        }

        [Fact]
        public async Task AddCartItemAsync_WithValidItem_ReturnsItem()
        {
            var ctx = CreateInMemoryContext(nameof(AddCartItemAsync_WithValidItem_ReturnsItem));
            var repo = new CartRepository(ctx);

            var cart = new Cart { CartId = 1, UserId = 1, BasicSiteId = 1 };
            await ctx.Carts.AddAsync(cart);
            await ctx.SaveChangesAsync();

            var item = new CartItem { CartId = 1, ProductId = 1, PlatformId = 1 };
            var result = await repo.AddCartItemAsync(item);

            Assert.True(result.CartItemId > 0);
        }

        [Fact]
        public async Task GetCartByIdAsync_WithValidId_ReturnsCart()
        {
            var ctx = CreateInMemoryContext(nameof(GetCartByIdAsync_WithValidId_ReturnsCart));
            var repo = new CartRepository(ctx);

            var cart = new Cart { CartId = 1, UserId = 1, BasicSiteId = 1 };
            await ctx.Carts.AddAsync(cart);
            await ctx.SaveChangesAsync();

            var result = await repo.GetCartByIdAsync(1);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetCartItemsByCartIdAsync_WithExistingItems_ReturnsItems()
        {
            var ctx = CreateInMemoryContext(nameof(GetCartItemsByCartIdAsync_WithExistingItems_ReturnsItems));
            var repo = new CartRepository(ctx);

            await ctx.MainCategories.AddAsync(new MainCategory
            {
                MainCategoryId = 1,
                MainCategoryName = "Business",
                MainCategoryPrompt = "Business prompt"
            });
            await ctx.SubCategories.AddAsync(new SubCategory
            {
                SubCategoryId = 1,
                MainCategoryId = 1,
                SubCategoryName = "Landing",
                SubCategoryPrompt = "Landing page prompt",
                ImageUrl = "img",
                CategoryDescription = "desc"
            });
            await ctx.Products.AddAsync(new Product
            {
                ProductId = 1,
                SubCategoryId = 1,
                ProductName = "Starter",
                Price = 10,
                ProductPrompt = "Product prompt"
            });
            await ctx.Platforms.AddAsync(new Platform
            {
                PlatformId = 1,
                PlatformName = "Webflow",
                PlatformPrompt = "Platform prompt"
            });

            var cart = new Cart { CartId = 1, UserId = 1, BasicSiteId = 1 };
            await ctx.Carts.AddAsync(cart);
            await ctx.CartItems.AddAsync(new CartItem { CartId = 1, ProductId = 1, PlatformId = 1 });
            await ctx.SaveChangesAsync();

            var result = await repo.GetCartItemsByCartIdAsync(1);

            Assert.NotEmpty(result);
        }

        #endregion

        #region Unhappy Paths

        [Fact]
        public async Task GetCartByIdAsync_NonExistentId_ReturnsNull()
        {
            var ctx = CreateInMemoryContext(nameof(GetCartByIdAsync_NonExistentId_ReturnsNull));
            var repo = new CartRepository(ctx);

            var result = await repo.GetCartByIdAsync(9999);

            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteCartAsync_NonExistentCart_ReturnsFalse()
        {
            var ctx = CreateInMemoryContext(nameof(DeleteCartAsync_NonExistentCart_ReturnsFalse));
            var repo = new CartRepository(ctx);

            var result = await repo.DeleteCartAsync(9999);

            Assert.False(result);
        }

        [Fact]
        public async Task ClearCartItemsAsync_WithNoItems_ReturnsFalse()
        {
            var ctx = CreateInMemoryContext(nameof(ClearCartItemsAsync_WithNoItems_ReturnsFalse));
            var repo = new CartRepository(ctx);

            var result = await repo.ClearCartItemsAsync(9999);

            Assert.False(result);
        }

        [Fact]
        public async Task ClearCartItemsAsync_OnlyDeletesActiveItems()
        {
            var ctx = CreateInMemoryContext(nameof(ClearCartItemsAsync_OnlyDeletesActiveItems));
            var repo = new CartRepository(ctx);

            // Seed data
            var cart = new Cart { CartId = 1, UserId = 1 };
            var activeItem = new CartItem { CartItemId = 1, CartId = 1, ProductId = 1, IsActive = true};
            var inactiveItem = new CartItem { CartItemId = 2, CartId = 1, ProductId = 2, IsActive = false };

            ctx.Carts.Add(cart);
            ctx.CartItems.Add(activeItem);
            ctx.CartItems.Add(inactiveItem);
            await ctx.SaveChangesAsync();

            // Act
            var result = await repo.ClearCartItemsAsync(1);

            // Assert
            Assert.True(result);
            var remainingItems = await ctx.CartItems.Where(ci => ci.CartId == 1).ToListAsync();
            Assert.Single(remainingItems);
            Assert.False(remainingItems.First().IsActive);
        }

        #endregion
    }
}
