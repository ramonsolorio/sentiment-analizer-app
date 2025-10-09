namespace SentimentAnalyzerApp.Services
{
    public interface ISentimentService
    {
        Task<SentimentResponse> AnalyzeSentimentAsync(SentimentRequest request);
    }
}