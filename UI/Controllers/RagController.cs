using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UI.Services;

namespace UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RagController : ControllerBase
    {
        private readonly IRagService _ragService;

        public RagController(IRagService ragService)
        {
            _ragService = ragService;
        }

        public class AskRequest { public string Query { get; set; } = string.Empty; }

        [HttpPost("answer")]
        public async Task<IActionResult> Answer([FromBody] AskRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Query)) return BadRequest("Query is required");
            var ans = await _ragService.GetAnswerAsync(req.Query);
            return Ok(new { answer = ans });
        }
    }
}
