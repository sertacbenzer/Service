using System.Text;
using System.Text.Json;

namespace UI.Services;

public class ChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public ChatService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<string> GetAnswerAsync(string question)
    {
        try
        {
            var requestBody = new { question };
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/chat/answer", content);
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseText);
            var answer = jsonDoc.RootElement.GetProperty("answer").GetString() ?? "Cevap alınamadı";

            return answer;
        }
        catch (Exception ex)
        {
            return $"Hata oluştu: {ex.Message}";
        }
    }
}
