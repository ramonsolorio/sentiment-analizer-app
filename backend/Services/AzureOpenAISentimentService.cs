using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using SentimentAnalyzerApp.Models;
using OpenAI.Chat;

namespace SentimentAnalyzerApp.Services
{
    public class AzureOpenAISentimentService : ISentimentService
    {
        private readonly ChatClient _chatClient;

        public AzureOpenAISentimentService(string endpoint, string deploymentName)
        {
            try
            {
                Console.WriteLine($"Initializing Azure OpenAI client...");
                Console.WriteLine($"Endpoint: {endpoint}");
                Console.WriteLine($"Deployment: {deploymentName}");

                var credential = new DefaultAzureCredential();
                var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
                _chatClient = azureClient.GetChatClient(deploymentName);
                
                Console.WriteLine("Azure OpenAI client initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Azure OpenAI client: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<SentimentResponse> AnalyzeSentimentAsync(SentimentRequest request)
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a sentiment analysis assistant. Analyze the sentiment of the given text and respond with only one word: Positive, Negative, or Neutral."),
                new UserChatMessage(request.Text)
            };

            var completion = await _chatClient.CompleteChatAsync(messages);
            var sentiment = completion.Value.Content[0].Text.Trim();

            return new SentimentResponse
            {
                Sentiment = sentiment,
                Score = 1.0,
                Message = "Analysis completed successfully"
            };
        }
    }
}