using DTO;
using Entities;
using Microsoft.AspNetCore.Mvc;
using Services;
using Zxcvbn;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlatformsController : ControllerBase
    {
        private readonly IPlatformService _platformService;
        public PlatformsController(IPlatformService platformService) {
            _platformService = platformService;
        }

        // GET: api/<PlatformsController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlatformsDTO>>> GetPlatformsAsync()
        {
           IEnumerable < PlatformsDTO > platformList = await _platformService.GetPlatformsAsync();
            if (platformList == null)
            {
                return NoContent();
            }
            else
                return Ok(platformList);
        }

        // POST api/<PlatformsController>
        [HttpPost]
        public async Task<ActionResult<PlatformsDTO>> AddPlatformAsync([FromBody] string platformName)
        {
            var PlatformForReturn= await _platformService.AddPlatformAsync(platformName);
            if (PlatformForReturn == null)
            {
                 return Conflict("Platform already exists in the system."); 
            }
            return CreatedAtAction(nameof(GetPlatformsAsync), new { id = PlatformForReturn.PlatformId }, PlatformForReturn);
        }

        // PUT api/<PlatformsController>/5
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePlatformAsync(int id, [FromBody] PlatformsDTO platform)
        {
            if (id <= 0 || platform == null)
                return BadRequest();

            if (platform.PlatformId != id)
                return BadRequest(new { message = "Payload id does not match route id." });

            var existing = await _platformService.GetPlatformByIdAsync(id);
            if (existing == null)
                return NotFound();

            var success = await _platformService.UpdatePlatformAsync(id, platform);
            if (!success)
                return BadRequest(new { message = "Unable to update platform - name may already be in use." });

            return NoContent();
        }

        // DELETE api/<PlatformsController>/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest();

            try
            {
                bool flag = await _platformService.DeletePlatformAsync(id);
                if (flag)
                    return NoContent();
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
