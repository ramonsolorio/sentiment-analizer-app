using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using SentimentAnalyzerApp.Models;

namespace SentimentAnalyzerApp.Services
{
    public class ApplicationInsightsTelemetryService : ITelemetryService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<ApplicationInsightsTelemetryService> _logger;

        public ApplicationInsightsTelemetryService(
            TelemetryClient telemetryClient,
            ILogger<ApplicationInsightsTelemetryService> logger)
        {
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        public void TrackSentimentAnalysis(SentimentResponse sentimentResponse)
        {
            try
            {
                // Enviar evento personalizado
                var eventTelemetry = new EventTelemetry("SentimentAnalyzed");
                eventTelemetry.Properties.Add("Sentiment", sentimentResponse.Sentiment);
                eventTelemetry.Properties.Add("Message", sentimentResponse.Message);
                eventTelemetry.Metrics.Add("Score", sentimentResponse.Score);

                _telemetryClient.TrackEvent(eventTelemetry);

                // Enviar métrica personalizada para el sentimiento
                _telemetryClient.GetMetric($"Sentiment_{sentimentResponse.Sentiment}").TrackValue(1);

                _logger.LogInformation(
                    "Sentiment tracked: {Sentiment} with score {Score}",
                    sentimentResponse.Sentiment,
                    sentimentResponse.Score);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking sentiment telemetry");
            }
        }

    }
}
