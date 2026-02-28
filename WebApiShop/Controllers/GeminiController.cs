using DTO;
using Microsoft.AspNetCore.Mvc;
using Services;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeminiController : ControllerBase
    {

        private readonly IGeminiService _geminiService;

        public GeminiController(IGeminiService geminiService) => _geminiService = geminiService;

        // POST: api/<GeminiController>/userProduct
        [HttpPost("userProduct")]
        public async Task<ActionResult<GeminiPromptDTO>> CreateUserPromptForProductAsync([FromBody] GeminiInputDTO request)
        {
            try
            {
                GeminiPromptDTO result = await _geminiService.AddGeminiForUserProductAsync(
                    request.SubCategoryId.HasValue ? (int)request.SubCategoryId.Value : null,
                    request.UserRequest);

                return CreatedAtAction(nameof(GetPromptByIdAsync), new { id = result.PromptId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/<GeminiController>/subCategory
        [HttpPost("subCategory")]
        public async Task<ActionResult<GeminiPromptDTO>> CreateUserPromptForCategoryAsync([FromBody] GeminiInputDTO request)
        {
            if (!request.SubCategoryId.HasValue)
            {
                return BadRequest("SubCategoryId is required for category prompt generation");
            }

            try
            {
                GeminiPromptDTO result = await _geminiService.AddGeminiForUserCategoryAsync(
                    (int)request.SubCategoryId.Value,
                    request.UserRequest);

                return CreatedAtAction(nameof(GetPromptByIdAsync), new { id = result.PromptId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/<GeminiController>/basicSite
        [HttpPost("basicSite")]
        public async Task<ActionResult<GeminiPromptDTO>> CreatePromptForBasicSiteAsync([FromBody] GeminiUserRequestDTO request)
        {
            try
            {
                GeminiPromptDTO result = await _geminiService.AddGeminiForBasicSiteAsync(request.UserRequest);
                return CreatedAtAction(nameof(GetPromptByIdAsync), new { id = result.PromptId }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET api/<GeminiController>/5
        [HttpGet("{id:long}")]
        public async Task<ActionResult<GeminiPromptDTO>> GetPromptByIdAsync(long id)
        {
            GeminiPromptDTO? gemini = await _geminiService.GetByIdPromptAsync(id);

            if(gemini == null)
            {
                return NoContent();
            }
            return Ok(gemini);
        }

        // PUT api/<GeminiController>/userProduct/5
        [HttpPut("/userProduct/{promptId:long}")]
        public async Task<ActionResult<GeminiPromptDTO>> UpdatePromptForProductAsync(long promptId, [FromBody] GeminiUserRequestDTO request)
        {
            try
            {
                GeminiPromptDTO result = await _geminiService.UpdateGeminiForUserProductAsync(promptId, request.UserRequest);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT api/<GeminiController>/suCategory/5
        [HttpPut("/suCategory/{promptId:long}")]
        public async Task<ActionResult<GeminiPromptDTO>> UpdatePromptForCategoryAsync(long promptId, [FromBody] GeminiUserRequestDTO request)
        {
            try
            {
                GeminiPromptDTO result = await _geminiService.UpdateGeminiForUserCategoryAsync(promptId, request.UserRequest);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT api/<GeminiController>/basicSite/5
        [HttpPut("/basicSite/{promptId:long}")]
        public async Task<ActionResult<GeminiPromptDTO>> UpdatePromptForBasicSiteAsync(long promptId, [FromBody] GeminiUserRequestDTO request)
        {
            try
            {
                GeminiPromptDTO result = await _geminiService.UpdateGeminiForBasicSiteAsync(promptId, request.UserRequest);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE api/<GeminiController>/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(long id)
        {
            try
            {
                await _geminiService.DeletePromptAsync(id);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
