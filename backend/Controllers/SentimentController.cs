using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SentimentAnalyzerApp.Models;
using SentimentAnalyzerApp.Services;

namespace SentimentAnalyzerApp.Controllers
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

            var response = await _sentimentService.AnalyzeSentimentAsync(request);
            return Ok(response);
        }
    }
}