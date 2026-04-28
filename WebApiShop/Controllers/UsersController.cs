using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET api/<UsersController>
        [HttpGet]
        [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetAllAsync()
        {
            var users = await _userService.GetAllAsync();
            if (users == null || !users.Any())
            {
                return NotFound($"No userOrders found ");
            }
            return Ok(users);
        }

        // GET api/<UsersController>/5
        [HttpGet("{id}")]
        [Authorize] 
        public async Task<ActionResult<UserProfileDTO>> GetByIdAsync(int id)
        {
            UserProfileDTO? user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }
            return Ok(user);
        }

        // GET api/<UsersController>/5/orders
        [HttpGet("{userId}/orders")]
        [Authorize] 
        public async Task<ActionResult<IEnumerable<UserProfileDTO>>> GetAllOrdersAsync(int userId)
        {
            var userOrders = await _userService.GetAllOrdersAsync(userId);
            if (userOrders == null || !userOrders.Any())
            {
                return NotFound($"No orders found for user with ID {userId}");
            }
            return Ok(userOrders);
        }

        // POST api/<UsersController>
        // Moved to POST api/auth/register — login now issues an HttpOnly JWT cookie.

        // PUT api/<UsersController>/5
        [HttpPut("{id}")]
        [Authorize] 
        public async Task<ActionResult> UpdateAsync(int id, [FromBody] UpdateUserDTO dto)
        {
            var isUpdated = await _userService.UpdateAsync(id, dto);
            if (isUpdated == null)
            {
                return NotFound($"User with ID {id} not found or update failed due to weak password");
            }
            return Ok(dto);
        }
    }
}
