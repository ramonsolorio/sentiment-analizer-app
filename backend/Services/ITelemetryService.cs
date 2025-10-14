using SentimentAnalyzerApp.Models;

namespace SentimentAnalyzerApp.Services
{
    public interface ITelemetryService
    {
        void TrackSentimentAnalysis(SentimentResponse sentimentResponse);
    }
}
