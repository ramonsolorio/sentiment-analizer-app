using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class AzureOpenAISentimentService : ISentimentService
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;

    public AzureOpenAISentimentService(HttpClient httpClient, string endpoint, string apiKey)
    {
        _httpClient = httpClient;
        _endpoint = endpoint;
        _apiKey = apiKey;
    }

    public async Task<SentimentResponse> AnalyzeSentimentAsync(SentimentRequest request)
    {
        var requestBody = new
        {
            prompt = request.Text,
            max_tokens = 60,
            temperature = 0.5
        };

        var jsonRequestBody = JsonConvert.SerializeObject(requestBody);
        var httpContent = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");
        httpContent.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.PostAsync(_endpoint, httpContent);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var sentimentResponse = JsonConvert.DeserializeObject<SentimentResponse>(jsonResponse);

        return sentimentResponse;
    }
}