using System.Threading.Tasks;
using SentimentAnalyzerApp.Models;

namespace SentimentAnalyzerApp.Services
{
    public interface ISentimentService
    {
        Task<SentimentResponse> AnalyzeSentimentAsync(SentimentRequest request);
    }
}