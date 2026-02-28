using DTO;
using Entities;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BasicSiteController : ControllerBase
    {
        private readonly IBasicSiteService _basicSiteService;
        public BasicSiteController(IBasicSiteService basicSitesServise)
        {
            _basicSiteService = basicSitesServise;

        }
      
        // GET api/<BasicSiteController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BasicSiteDTO>> GetBasicSiteByIdAsync(int id)
        {
            BasicSiteDTO basicSite = await _basicSiteService.GetBasicSiteByIdAsync(id);
            if (basicSite == null)
            {
                return NoContent();
            }
            return Ok(basicSite);
        }

        // POST api/<BasicSiteController>
        [HttpPost]
        async public Task<ActionResult<BasicSiteDTO>> AddBasicSiteAsync([FromBody] AddBasicSiteDTO basicSite)
        {
            BasicSiteDTO basicSiteConstructedObject = await _basicSiteService.AddBasicSiteAsync(basicSite);
            return CreatedAtAction(nameof(GetBasicSiteByIdAsync), new { id = basicSiteConstructedObject.BasicSiteId }, basicSiteConstructedObject);
        
        }

        // PUT api/<BasicSiteController>/5
        [HttpPut("{id}")]
        async public Task UpdateBasicSiteAsync(int id, [FromBody] UpdateBasicSiteDTO basicSite)
        {
            await _basicSiteService.UpdateBasicSiteAsync(id, basicSite);
        }
    }
}
