using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using SentimentAnalyzerApp.Models;
using SentimentAnalyzerApp.Services;

namespace SentimentAnalyzerApp.Services
{
    public class AzureOpenAISentimentService : ISentimentService
    {
        private readonly AzureOpenAIClient _azureClient;
        private readonly string _deploymentName;

        public AzureOpenAISentimentService(string endpoint, string deploymentName)
        {
            var credential = new DefaultAzureCredential();
            _azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
            _deploymentName = deploymentName;
        }

        public async Task<SentimentResponse> AnalyzeSentimentAsync(SentimentRequest request)
        {
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage("You are a sentiment analysis assistant. Analyze the sentiment of the given text and respond with only one word: Positive, Negative, or Neutral."),
                    new ChatRequestUserMessage(request.Text)
                },
                MaxTokens = 60,
                Temperature = 0.5f
            };

            Response<ChatCompletions> response = await _azureClient.GetChatCompletionsAsync(chatCompletionsOptions);
            var sentiment = response.Value.Choices[0].Message.Content.Trim();

            return new SentimentResponse
            {
                Sentiment = sentiment
            };
        }
    }
}