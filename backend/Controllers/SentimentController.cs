using Microsoft.AspNetCore.Mvc;
using System;
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
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Text))
                {
                    return BadRequest("Invalid request. Text is required.");
                }

                var response = await _sentimentService.AnalyzeSentimentAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing sentiment: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Error analyzing sentiment", message = ex.Message });
            }
        }
    }
}