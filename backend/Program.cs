using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using SentimentAnalyzerApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING") 
        ?? builder.Configuration["ApplicationInsights:ConnectionString"];
});

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson();

// Add health checks
builder.Services.AddHealthChecks();

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

// Register telemetry service
builder.Services.AddSingleton<ITelemetryService, ApplicationInsightsTelemetryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// IMPORTANT: CORS must be before Authorization
app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthorization();

// Map health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

Console.WriteLine($"Backend starting...");
Console.WriteLine($"Azure OpenAI Endpoint: {azureOpenAIEndpoint}");
Console.WriteLine($"Azure OpenAI Deployment: {azureOpenAIDeployment}");

app.Run();
