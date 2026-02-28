using AutoMapper;
using DTO;
using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;

namespace Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IBasicSiteService _basicSiteService;
        private readonly IMapper _mapper;

        public CartService(ICartRepository cartRepository, IBasicSiteService basicSiteService, IMapper mapper)
        {
            _cartRepository = cartRepository;
            _basicSiteService = basicSiteService;
            _mapper = mapper;
        }

        public async Task<CartDTO?> GetCartByIdAsync(int cartId)
        {
            if (cartId <= 0)
            {
                throw new InvalidOperationException("Invalid cart ID.");
            }

            var cart = await _cartRepository.GetCartByIdAsync(cartId);
            if (cart == null)
            {
                return null;
            }

            var cartItems = await _cartRepository.GetCartItemsByCartIdAsync(cartId);
            var mappedItems = _mapper.Map<List<CartItemDTO>>(cartItems);
            var basicSitePrice = await GetBasicSitePriceOrZeroAsync(cart.BasicSiteId);

            return new CartDTO
            {
                CartId = cart.CartId,
                UserId = cart.UserId,
                BasicSiteId = cart.BasicSiteId,
                CartItems = mappedItems,
                BasicSitePrice = basicSitePrice,
                TotalPrice = mappedItems.Sum(item => item.Price) + basicSitePrice,
                BasicSiteName = cart.BasicSite?.SiteName,
                BasicSiteUserDescription = cart.BasicSite?.UserDescreption
            };
        }

        public async Task<CartItemDTO?> GetCartItemByIdAsync(int id)
        {
            var cartItem = await _cartRepository.GetCartItemByIdAsync(id);

            if (cartItem == null)
                return null;

            return _mapper.Map<CartItemDTO>(cartItem);
        }

        public async Task<IEnumerable<CartItemDTO>?> GetCartItemsByCartIdAsync(int cartId)
        {
            var cartItems = await _cartRepository.GetCartItemsByCartIdAsync(cartId);

            if (cartItems == null)
                return null;

            return _mapper.Map<IEnumerable<CartItemDTO>>(cartItems);
        }

        public async Task<CartItemDTO> AddCartItemForUserAsync(int userId, AddCartItemDTO dto)
        {
            ValidateAddCartItemRequest(userId, dto);
            await EnsureProductExistsAsync((int)dto.ProductId);

            var cart = await EnsureUserCartAsync(userId);
            await EnsureCartDoesNotContainProductAsync((int)cart.CartId, (int)dto.ProductId);
            var cartItem = BuildCartItem((int)cart.CartId, dto);
            var created = await AddCartItemWithDuplicateHandlingAsync(cartItem);
            return _mapper.Map<CartItemDTO>(created);
        }

        public async Task<GuestCartImportResultDTO> ImportGuestCartAsync(int userId, ImportGuestCartDTO dto)
        {
            ValidateGuestCartImportRequest(userId, dto);

            var cart = await EnsureUserCartAsync(userId);
            await ValidateGuestCartItemsForImportAsync((int)cart.CartId, dto.Items);
            int addedCount = await AddGuestCartItemsAsync((int)cart.CartId, dto.Items);

            return CreateGuestCartImportResult((int)cart.CartId, addedCount);
        }

        private static void ValidateGuestCartImportRequest(int userId, ImportGuestCartDTO dto)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Invalid user ID.");
            }

            if (dto == null)
            {
                throw new InvalidOperationException("Request body is required.");
            }

            if (dto.Items == null || dto.Items.Count == 0)
            {
                throw new InvalidOperationException("Guest cart is empty.");
            }
        }

        private async Task ValidateGuestCartItemsForImportAsync(int cartId, List<ImportGuestCartItemDTO> items)
        {
            var productIdsInPayload = new HashSet<int>();

            foreach (var item in items)
            {
                if (item.ProductId <= 0)
                {
                    throw new InvalidOperationException("Each guest cart item must include a valid ProductId.");
                }

                if (!await _cartRepository.ProductExistsAsync(item.ProductId))
                {
                    throw new InvalidOperationException($"Product {item.ProductId} does not exist.");
                }

                if (!productIdsInPayload.Add(item.ProductId))
                {
                    throw new InvalidOperationException($"Product {item.ProductId} appears more than once in the request.");
                }

                var existingItem = await _cartRepository.GetByCartAndProductIdAsync(cartId, item.ProductId);
                if (existingItem != null)
                {
                    throw new InvalidOperationException($"Product {item.ProductId} already exists in this cart.");
                }
            }
        }

        private async Task<int> AddGuestCartItemsAsync(int cartId, List<ImportGuestCartItemDTO> items)
        {
            int addedCount = 0;

            foreach (var item in items)
            {
                var cartItem = new CartItem
                {
                    CartId = cartId,
                    ProductId = item.ProductId,
                    PlatformId = item.PlatformId,
                    PromptId = item.PromptId,
                    IsActive = true
                };

                try
                {
                    await _cartRepository.AddCartItemAsync(cartItem);
                }
                catch (DbUpdateException ex) when (IsDuplicateCartItemViolation(ex))
                {
                    throw new InvalidOperationException($"Product {item.ProductId} already exists in this cart.");
                }

                addedCount++;
            }

            return addedCount;
        }

        private static GuestCartImportResultDTO CreateGuestCartImportResult(int cartId, int addedCount)
        {
            return new GuestCartImportResultDTO(
                cartId,
                addedCount,
                0,
                new List<int>()
            );
        }

        public async Task<CartItemDTO?> UpdateCartItemAsync(UpdateCartItemDTO dto)
        {
            if (dto == null)
            {
                throw new InvalidOperationException("Request body is required.");
            }

            if (dto.CartItemId <= 0)
            {
                throw new InvalidOperationException("CartItemId is required.");
            }

            var cartItem = new CartItem
            {
                CartItemId = dto.CartItemId,
                PlatformId = dto.PlatformId,
                IsActive = dto.IsActive
            };

            var updated = await _cartRepository.UpdateCartItemAsync(cartItem);
            if (updated == null)
            {
                return null;
            }

            return _mapper.Map<CartItemDTO>(updated);
        }

        public async Task<CartDTO?> UpdateCartAsync(int cartId, UpdateCartDTO dto)
        {
            ValidateUpdateCartRequest(cartId, dto);
            await ValidateBasicSiteForUpdateAsync(dto.BasicSiteId);

            var updatedCart = await _cartRepository.UpdateCartAsync(BuildCartUpdateEntity(cartId, dto));

            if (updatedCart == null)
            {
                return null;
            }

            return await BuildUpdatedCartDtoAsync(updatedCart);
        }

        public async Task<bool> DeleteCartItemAsync(int cartItemId)
        {
            return await _cartRepository.DeleteCartItemAsync(cartItemId);
        }

        public async Task<bool> ClearCartAsync(int cartId)
        {
            return await _cartRepository.ClearCartItemsAsync(cartId);
        }

        private async Task<Cart> EnsureUserCartAsync(int userId)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Invalid user ID.");
            }

            if (!await _cartRepository.UserExistsAsync(userId))
            {
                throw new InvalidOperationException("User does not exist.");
            }

            var existingCart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (existingCart != null)
            {
                return existingCart;
            }

            var newCart = new Cart
            {
                UserId = userId,
                BasicSiteId = null,
                TotalPrice = 0
            };

            return await _cartRepository.CreateUserCartAsync(newCart);
        }

        private static void ValidateAddCartItemRequest(int userId, AddCartItemDTO dto)
        {
            if (userId <= 0)
            {
                throw new InvalidOperationException("Invalid user ID.");
            }

            if (dto == null)
            {
                throw new InvalidOperationException("Request body is required.");
            }

            if (dto.ProductId <= 0)
            {
                throw new InvalidOperationException("ProductId is required.");
            }
        }

        private async Task EnsureProductExistsAsync(int productId)
        {
            if (!await _cartRepository.ProductExistsAsync(productId))
            {
                throw new InvalidOperationException("Product does not exist.");
            }
        }

        private async Task EnsureCartDoesNotContainProductAsync(int cartId, int productId)
        {
            var existingItem = await _cartRepository.GetByCartAndProductIdAsync(cartId, productId);
            if (existingItem != null)
            {
                throw new InvalidOperationException("Product already exists in this cart.");
            }
        }

        private static CartItem BuildCartItem(int cartId, AddCartItemDTO dto)
        {
            return new CartItem
            {
                CartId = cartId,
                ProductId = dto.ProductId,
                PlatformId = dto.PlatformId,
                IsActive = true
            };
        }

        private async Task<CartItem> AddCartItemWithDuplicateHandlingAsync(CartItem cartItem)
        {
            try
            {
                return await _cartRepository.AddCartItemAsync(cartItem);
            }
            catch (DbUpdateException ex) when (IsDuplicateCartItemViolation(ex))
            {
                throw new InvalidOperationException("Product already exists in this cart.");
            }
        }

        private static void ValidateUpdateCartRequest(int cartId, UpdateCartDTO dto)
        {
            if (cartId <= 0)
            {
                throw new InvalidOperationException("Invalid cart ID.");
            }

            if (dto == null)
            {
                throw new InvalidOperationException("Request body is required.");
            }

            if (dto.BasicSiteId.HasValue && dto.BasicSiteId.Value <= 0)
            {
                throw new InvalidOperationException("BasicSiteId must be a positive value when provided.");
            }
        }

        private async Task ValidateBasicSiteForUpdateAsync(int? basicSiteId)
        {
            if (basicSiteId.HasValue && !await _cartRepository.BasicSiteExistsAsync(basicSiteId.Value))
            {
                throw new InvalidOperationException("Basic site does not exist.");
            }
        }

        private static Cart BuildCartUpdateEntity(int cartId, UpdateCartDTO dto)
        {
            return new Cart
            {
                CartId = cartId,
                BasicSiteId = dto.BasicSiteId
            };
        }

        private async Task<CartDTO> BuildUpdatedCartDtoAsync(Cart updatedCart)
        {
            var basicSitePrice = await GetBasicSitePriceOrZeroAsync(updatedCart.BasicSiteId);
            var itemsTotal = await GetItemsTotalByCartIdAsync((int)updatedCart.CartId);

            return new CartDTO
            {
                CartId = updatedCart.CartId,
                UserId = updatedCart.UserId,
                BasicSiteId = updatedCart.BasicSiteId,
                BasicSitePrice = basicSitePrice,
                TotalPrice = itemsTotal + basicSitePrice,
                CartItems = new List<CartItemDTO>(),
                BasicSiteName = string.Empty,
                BasicSiteUserDescription = string.Empty
            };
        }

        private static bool IsDuplicateCartItemViolation(DbUpdateException ex)
        {
            if (ex.InnerException == null)
            {
                return false;
            }

            var message = ex.InnerException.Message;
            return message.Contains("UX_CartItems_Cart_Product", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || message.Contains("unique", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<double> GetBasicSitePriceOrZeroAsync(long? basicSiteId)
        {
            if (!basicSiteId.HasValue)
            {
                return 0;
            }

            return await _basicSiteService.GetBasicSitePriceAsync(basicSiteId.Value);
        }

        private async Task<double> GetItemsTotalByCartIdAsync(int cartId)
        {
            var cartItems = await _cartRepository.GetCartItemsByCartIdAsync(cartId);
            var mappedItems = _mapper.Map<List<CartItemDTO>>(cartItems);
            return mappedItems.Sum(item => item.Price);
        }

    }
}