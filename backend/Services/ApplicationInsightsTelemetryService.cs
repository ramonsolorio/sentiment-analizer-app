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

                // Si es negativo, enviar métrica específica para escalado
                if (sentimentResponse.Sentiment.Equals("Negative", StringComparison.OrdinalIgnoreCase))
                {
                    TrackNegativeSentiment(sentimentResponse.Message, sentimentResponse.Score);
                }

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

        public void TrackNegativeSentiment(string text, double score)
        {
            try
            {
                // Métrica crítica para escalado automático
                // Esta métrica se usa en la regla de escalado de ACA
                _telemetryClient.GetMetric("NegativeSentimentCount").TrackValue(1);

                // Enviar evento de alta prioridad
                var eventTelemetry = new EventTelemetry("NegativeSentimentDetected");
                eventTelemetry.Properties.Add("Text", text);
                eventTelemetry.Properties.Add("Severity", score > 0.8 ? "High" : "Medium");
                eventTelemetry.Metrics.Add("NegativeScore", score);

                _telemetryClient.TrackEvent(eventTelemetry);

                _logger.LogWarning(
                    "Negative sentiment detected with score {Score}. Text: {Text}",
                    score,
                    text.Length > 100 ? text.Substring(0, 100) + "..." : text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking negative sentiment");
            }
        }
    }
}
