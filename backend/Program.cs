using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using SentimentAnalyzerApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Get Azure OpenAI configuration from environment variables or appsettings
var azureOpenAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") 
    ?? builder.Configuration["AzureOpenAI:Endpoint"] 
    ?? "https://eus2-devia-openia-2w36.openai.azure.com/";

var azureOpenAIDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") 
    ?? builder.Configuration["AzureOpenAI:DeploymentName"] 
    ?? "gpt-4.1";

// Register the sentiment service
builder.Services.AddSingleton<ISentimentService>(sp => 
    new AzureOpenAISentimentService(azureOpenAIEndpoint, azureOpenAIDeployment));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();