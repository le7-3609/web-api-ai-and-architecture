using AutoMapper;
using DTO;
using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;

namespace Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;

        private readonly IMapper _mapper;
        private readonly ICartService _cartService;
        private readonly ILogger<OrderService> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IOrderPromptBuilder _promptBuilder;

        public OrderService(IOrderRepository orderRepository, IMapper mapper, ICartService cartService, ILogger<OrderService> logger, IProductRepository productRepository, IHostEnvironment hostEnvironment, IOrderPromptBuilder promptBuilder)
        {
            _orderRepository = orderRepository;
            _mapper = mapper;
            _cartService = cartService;
            _logger = logger;
            _productRepository = productRepository;
            _hostEnvironment = hostEnvironment;
            _promptBuilder = promptBuilder;
        }
        private async Task<double> CalculateRealSumAsync(List<CartItemDTO> items, double basicSitePrice)
        {
            double total = basicSitePrice;
            foreach (var item in items)
            {
                var product = await _productRepository.GetProductByIdAsync((int)item.ProductId);
                if (product == null)
                {
                    _logger.LogError("Security Warning: Product ID {ProductId} not found during validation.", item.ProductId);
                    throw new KeyNotFoundException("Product not found");
                }
                total += product.Price;
            }
            return total;
        }

        private bool IsCartTotalConsistent(CartDTO cartDto, double expectedSum)
        {
            return Math.Abs(cartDto.TotalPrice - expectedSum) < 0.01;
        }

        public async Task<(IEnumerable<OrderDetailsDTO> Orders, double Total)> GetOrdersAsync()
        {
            double total = 0;
            var orders = await _orderRepository.GetOrdersAsync();
            foreach (var order in orders)
            {
                total += order.OrderSum;
            }
            var orderDtos = _mapper.Map<IEnumerable<OrderDetailsDTO>>(orders);
            return (orderDtos, total);
        }

        public async Task<IEnumerable<StatusesDTO>> GetStatusesAsync()
        {
            var statuses = await _orderRepository.GetStatusesAsync();
            return _mapper.Map<IEnumerable<StatusesDTO>>(statuses);

        }

        public async Task<OrderDetailsDTO> AddOrderFromCartAsync(int cartId)
        {
            ValidateCartId(cartId);
            var cartDto = await GetValidatedCartAsync(cartId);
            var expectedSum = await CalculateRealSumAsync(cartDto.CartItems, cartDto.BasicSitePrice);
            ValidateCartTotalConsistency(cartId, cartDto, expectedSum);
            var order = BuildOrderFromCart(cartDto, expectedSum);

            return await SaveOrderAndClearCartAsync(order, cartId);
        }

        private static void ValidateCartId(int cartId)
        {
            if (cartId <= 0)
            {
                throw new InvalidOperationException("Invalid cart ID.");
            }
        }

        private async Task<CartDTO> GetValidatedCartAsync(int cartId)
        {
            var cartDto = await _cartService.GetCartByIdAsync(cartId);
            if (cartDto == null)
            {
                throw new KeyNotFoundException("Cart not found.");
            }

            if (cartDto.CartItems == null || cartDto.CartItems.Count == 0)
            {
                throw new InvalidOperationException("The cart is empty.");
            }

            if (!cartDto.BasicSiteId.HasValue)
            {
                throw new InvalidOperationException("Cannot create order without BasicSiteId.");
            }

            return cartDto;
        }

        private void ValidateCartTotalConsistency(int cartId, CartDTO cartDto, double expectedSum)
        {
            if (!IsCartTotalConsistent(cartDto, expectedSum))
            {
                _logger.LogWarning("SECURITY ALERT: Cart total mismatch for Cart {CartId}. Cart.TotalPrice={CartTotalPrice}, Expected={ExpectedSum}",
                    cartId, cartDto.TotalPrice, expectedSum);
                throw new InvalidOperationException("Cart total validation failed.");
            }
        }

        private static Order BuildOrderFromCart(CartDTO cartDto, double expectedSum)
        {
            return new Order
            {
                UserId = cartDto.UserId,
                BasicSiteId = cartDto.BasicSiteId!.Value,
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                OrderSum = expectedSum,
                Status = 1,
                OrderItems = cartDto.CartItems.Select(item => new OrderItem
                {
                    PromptId = item.PromptId,
                    PlatformId = item.PlatformId ?? 1,
                    ProductId = item.ProductId
                }).ToList()
            };
        }

        private async Task<OrderDetailsDTO> SaveOrderAndClearCartAsync(Order order, int cartId)
        {
            order.Orderprompt = await _promptBuilder.BuildPromptAsync(order.BasicSiteId, order.OrderItems);
            var createdOrder = await _orderRepository.AddOrderAsync(order);
            await _cartService.ClearCartAsync(cartId);
            return _mapper.Map<OrderDetailsDTO>(createdOrder);
        }

        public async Task<OrderDetailsDTO?> GetByIdAsync(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                return null;
            }
            return _mapper.Map<OrderDetailsDTO>(order);
        }
       
        public async Task UpdateStatusAsync(OrderSummaryDTO dto)
        {
            var order = await _orderRepository.GetByIdAsync((int)dto.OrderId);
            var statuses = await _orderRepository.GetStatusesAsync();
            var status = statuses.FirstOrDefault(s =>
            string.Equals(s.StatusName, dto.StatusName, StringComparison.OrdinalIgnoreCase));
            order.Status = status.StatusId;
            await _orderRepository.UpdateStatusAsync(order);
        }
        public async Task<ReviewDTO?> AddReviewAsync(int orderId, AddReviewDTO dto)
        {
            var existingReview = await _orderRepository.GetReviewByOrderIdAsync(orderId);
            if (existingReview != null) return null;

            if (dto.Score < 1 || dto.Score > 5)
            {
                throw new ArgumentException("Review score must be between 1 and 5.");
            }

            string? relativePath = dto.Image != null ? await saveImageToFileSystem(dto.Image) : null;

            var review = _mapper.Map<Review>(dto);
            review.OrderId = orderId;
            review.ReviewImageUrl = relativePath; 

            var savedReview = await _orderRepository.AddReviewAsync(review);
            return _mapper.Map<ReviewDTO>(savedReview);
        }
        private async Task<string?> saveImageToFileSystem(IFormFile image)
        {
            if (image != null && image.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var uploadsFolder = Path.Combine(_hostEnvironment.ContentRootPath, "uploads");

                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                return "/uploads/" + fileName;
            }
            return null;
        }

        public async Task<ReviewDTO?> GetReviewByOrderIdAsync(int orderId)
        {
            var review = await _orderRepository.GetReviewByOrderIdAsync(orderId);
            if (review == null)
            {
                return null;
            }
            return _mapper.Map<ReviewDTO>(review);
        }
        public async Task UpdateReviewAsync(ReviewDTO dto)
        {
            var review = _mapper.Map<Review>(dto);
            await _orderRepository.UpdateReviewAsync(review);
        }
        public async Task<IEnumerable<OrderItemDTO>?> GetOrderItemsAsync(int orderId)
        {
            var orderItems = await _orderRepository.GetOrderItemsAsync(orderId);

            if (orderItems == null)
                return null;

            return _mapper.Map<IEnumerable<OrderItemDTO>>(orderItems);
        }

        public async Task<string?> GetOrderPromptAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            return order?.Orderprompt;
        }

        public async Task<IEnumerable<ReviewSummaryDTO>> GetAllReviewsAsync()
        {
            var reviews = await _orderRepository.GetAllReviewsAsync();
            return _mapper.Map<IEnumerable<ReviewSummaryDTO>>(reviews);
        }
    }
}
