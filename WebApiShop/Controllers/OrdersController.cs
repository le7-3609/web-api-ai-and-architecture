using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // GET api/<OrdersController>/5
        [HttpGet("{id}")]
        [Authorize] 
        public async Task<ActionResult<OrderDetailsDTO>> GetByIdAsync(int id)
        {
            OrderDetailsDTO order = await _orderService.GetByIdAsync(id);
            if (order == null)
            {
                return NoContent();
            }
            return Ok(order);
        }

        // GET api/<OrdersController>
        [HttpGet]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<IEnumerable<OrdersResponseDTO>>> GetOrdersAsync()
        {
            var orders = await _orderService.GetOrdersAsync();
            if (orders.Orders == null || !orders.Orders.Any())
            {
                return NoContent();
            }

            var response = new OrdersResponseDTO
            {
                Orders = orders.Orders,
                Total = orders.Total
            };

            return Ok(response);
        }

        // GET api/<OrdersController>/statuses
        [HttpGet("statuses")]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<IEnumerable<StatusesDTO>>> GetStatusesAsync()
        {
            var statuses = await _orderService.GetStatusesAsync();
            if (statuses == null || !statuses.Any())
            {
                return NoContent();
            }
            return Ok(statuses);
        }

        // POST api/Orders/carts/5
        [HttpPost("carts/{cartId}")]
        [Authorize] 
        public async Task<ActionResult<OrderDetailsDTO>> AddOrderAsync(int cartId)
        {
            try
            {
                OrderDetailsDTO newOrder = await _orderService.AddOrderFromCartAsync(cartId);
                return CreatedAtAction(nameof(GetByIdAsync), new { id = newOrder.OrderId }, newOrder);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT api/<OrdersController>/5
        [HttpPut]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult> UpdateStatusAsync([FromBody] OrderSummaryDTO dto)
        {
            await _orderService.UpdateStatusAsync(dto);
            return Ok();
        }

        // GET api/<OrdersController>/5/items
        [HttpGet("{orderId}/orderItems")]
        [Authorize] 
        public async Task<ActionResult<IEnumerable<OrderItemDTO>>> GetOrderItemsAsync(int orderId)
        {
            var orderItems = await _orderService.GetOrderItemsAsync(orderId);
            if (orderItems == null || !orderItems.Any())
            {
                return NotFound($"No order items found for Order ID {orderId}");
            }
            return Ok(orderItems);
        }

        // GET api/<OrdersController>/5/prompt
        [HttpGet("{orderId}/prompt")]
        [Authorize] 
        public async Task<ActionResult<string>> GetOrderPromptAsync(int orderId)
        {
            var prompt = await _orderService.GetOrderPromptAsync(orderId);
            if (prompt == null)
            {
                return NotFound($"Order {orderId} not found or has no prompt.");
            }
            return Ok(prompt);
        }
    }
}