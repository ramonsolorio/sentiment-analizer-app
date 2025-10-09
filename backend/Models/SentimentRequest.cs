using System.ComponentModel.DataAnnotations;

namespace SentimentAnalyzerApp.Models
{
    public class SentimentRequest
    {
        [Required]
        public string Text { get; set; } = string.Empty;
    }
}