namespace SentimentAnalyzerApp.Models
{
    public class SentimentResponse
    {
        public string Sentiment { get; set; } = string.Empty;
        public double Score { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}