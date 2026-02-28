using DTO;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusesController : ControllerBase
    {
        private readonly IStatusService _statusService;

        public StatusesController(IStatusService statusService)
        {
            _statusService = statusService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StatusesDTO>>> GetAllStatusesAsync()
        {
            var statuses = await _statusService.GetAllStatusesAsync();
            if (!statuses.Any())
                return NoContent();
            return Ok(statuses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StatusesDTO>> GetStatusByIdAsync(int id)
        {
            var status = await _statusService.GetStatusByIdAsync(id);
            if (status == null)
                return NotFound();
            return Ok(status);
        }
    }
}
