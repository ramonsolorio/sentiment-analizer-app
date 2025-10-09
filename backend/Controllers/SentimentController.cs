using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YourNamespace.Models; // Update with your actual namespace
using YourNamespace.Services; // Update with your actual namespace

namespace YourNamespace.Controllers // Update with your actual namespace
{
    [ApiController]
    [Route("api/[controller]")]
    public class SentimentController : ControllerBase
    {
        private readonly ISentimentService _sentimentService;

        public SentimentController(ISentimentService sentimentService)
        {
            _sentimentService = sentimentService;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeSentiment([FromBody] SentimentRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest("Invalid request.");
            }

            var response = await _sentimentService.AnalyzeSentimentAsync(request.Text);
            return Ok(response);
        }
    }
}