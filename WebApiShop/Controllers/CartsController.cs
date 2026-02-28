using DTO;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartsController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // GET api/Carts/items/5
        [HttpGet("items/{id}")]
        public async Task<ActionResult<CartItemDTO>> GetCartItemByIdAsync(int id)
        {
            var cartItem = await _cartService.GetCartItemByIdAsync(id);
            if (cartItem == null)
            {
                return NotFound($"CartItem with ID {id} not found");
            }
            return Ok(cartItem);
        }

        // GET api/Carts/5/items
        [HttpGet("{cartId}/items")]
        public async Task<ActionResult<IEnumerable<CartItemDTO>>> GetCartItemsByCartIdAsync(int cartId)
        {
            var cartItems = await _cartService.GetCartItemsByCartIdAsync(cartId);
            if (cartItems == null)
            {
                return NotFound($"Cart with ID {cartId} not found");
            }
            return Ok(cartItems);
        }

        // POST api/Carts/users/5/items 
        [HttpPost("users/{userId}/items")]
        public async Task<ActionResult<CartItemDTO>> AddItemToUserCartAsync(int userId, [FromBody] AddCartItemDTO dto)
        {
            try
            {
                var newCartItem = await _cartService.AddCartItemForUserAsync(userId, dto);
                return CreatedAtAction(nameof(GetCartItemsByCartIdAsync), new { cartId = newCartItem.CartId }, newCartItem);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST api/Carts/users/5/import-guest
        [HttpPost("users/{userId}/import-guest")]
        public async Task<ActionResult<GuestCartImportResultDTO>> ImportGuestCartAsync(int userId, [FromBody] ImportGuestCartDTO dto)
        {
            try
            {
                var result = await _cartService.ImportGuestCartAsync(userId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT api/Carts/items
        [HttpPut("items")]
        public async Task<ActionResult<CartItemDTO>> UpdateItemInCartAsync([FromBody] UpdateCartItemDTO dto)
        {
            try
            {
                var updated = await _cartService.UpdateCartItemAsync(dto);
                if (updated == null)
                {
                    return NotFound("Cart item not found");
                }

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT api/Carts/5
        [HttpPut("{id}")]
        public async Task<ActionResult<CartDTO>> UpdateCartAsync(int id, [FromBody] UpdateCartDTO dto)
        {
            try
            {
                var updated = await _cartService.UpdateCartAsync(id, dto);
                if (updated == null)
                {
                    return NotFound($"Cart with ID {id} not found");
                }

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE api/Carts/items/5
        [HttpDelete("items/{id}")]
        public async Task<ActionResult> RemoveItemFromCartAsync(int id)
        {
            var succeeded = await _cartService.DeleteCartItemAsync(id);
            if (!succeeded)
            {
                return NotFound($"Item with ID {id} not found");
            }
            return NoContent();
        }

        // DELETE api/Carts/5/clear
        [HttpDelete("{cartId}/clear")]
        public async Task<ActionResult> ClearCartAsync(int cartId)
        {
            var succeeded = await _cartService.ClearCartAsync(cartId);
            if (!succeeded)
            {
                return NotFound($"Cart with ID {cartId} could not be cleared");
            }
            return NoContent();
        }
    }
}