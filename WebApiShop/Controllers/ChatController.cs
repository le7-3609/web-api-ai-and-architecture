using DTO;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatBotService _chatBotService;

        public ChatController(IChatBotService chatBotService)
        {
            _chatBotService = chatBotService;
        }

        [HttpPost]
        public async Task<ActionResult<string>> SendMessageAsync([FromBody] ChatRequestDTO request)
        {
            try
            {
                string result = await _chatBotService.SendMessageAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
