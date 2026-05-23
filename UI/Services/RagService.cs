using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UI.Services
{
    public class RagService : IRagService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _connectionString;

        public RagService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _connectionString =   "Server=localhost,1433;Database=Orion;User Id=sa;Password=Ggrt190724;TrustServerCertificate=True;";
        }

        public async Task<string> GetAnswerAsync(string userQuery)
        {
            var retrievedDocs = await RetrieveAsync(userQuery, topK: 5);

            if (retrievedDocs.Count == 0)
                return "Üzgünüm, sorunuzla ilgili yeterli bilgi bulamadım.";

            string context = string.Join("\n\n---\n\n",
                retrievedDocs.Select((doc, i) => $"Doküman {i + 1}: {doc.Title}\n{doc.Content}"));

            string prompt = $"""
Sen çok titiz, doğru bilgi veren ve asla halüsinasyon yapmayan profesyonel bir Türk finansal eğitim asistanısın.

Kullanıcının sorusu: {userQuery}

KULLANABİLECEĞİN TEK BAĞLAM:
{context}

KESİN KURALLAR (Mutlaka uy, aksi takdirde cevap verme):
- Sadece yukarıdaki bağlamda geçen bilgileri kullan. Bağlamda olmayan hiçbir bilgi, tanım, örnek veya sayı uydurma.
- Tanım yaparken çok net ve doğru ol.
- Cevabı şu yapıya göre ver:
  1. Kısa ve net tanım
  2. Avantajlar (madde ile)
  3. Riskler (madde ile)
  4. Pratik bilgi (varsa)
- Sade, akıcı ve profesyonel Türkçe kullan.
- Kesinlikle uydurma bilgi ekleme.
- Sonunda mutlaka şu cümleyi ekle: "this metin sadece eğitim ve bilgilendirme amaçlıdır. Yatırım tavsiyesi niteliği taşımaz."

Cevap:
""";

            var requestBody = new
            {
                model = "llama3.1:8b",
                prompt = prompt,
                stream = false
            };

            var httpClient = _httpClientFactory.CreateClient("ollama");
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/api/generate", jsonContent);
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseText);
            var answer = jsonDoc.RootElement.GetProperty("response").GetString() ?? string.Empty;

            return answer.Trim();
        }

        private async Task<List<RetrievedDocument>> RetrieveAsync(string userQuery, int topK = 3)
        {
            var httpClient = _httpClientFactory.CreateClient("ollama");

            var embedRequestBody = new
            {
                model = "bge-m3:latest",
                input = new[] { userQuery }
            };

            var embedJsonContent = new StringContent(JsonSerializer.Serialize(embedRequestBody), Encoding.UTF8, "application/json");
            var embedResponse = await httpClient.PostAsync("/api/embed", embedJsonContent);
            embedResponse.EnsureSuccessStatusCode();

            var embedResponseText = await embedResponse.Content.ReadAsStringAsync();
            var embedJsonDoc = JsonDocument.Parse(embedResponseText);
            var embeddings = embedJsonDoc.RootElement.GetProperty("embeddings").EnumerateArray().First();

            var queryEmbedding = embeddings.EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray();

            var documents = new List<RetrievedDocument>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string embeddingVector = "[" + string.Join(",", queryEmbedding.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";

            string sql = """
                SELECT TOP(@topK) 
                    Id, Title, Content, Category, RiskLevel, TargetAudience, 
                    SourceType, CreatedDate,
                    VECTOR_DISTANCE('cosine', ContentVector, CAST(@queryEmbedding AS VECTOR(1024))) AS Distance
                FROM dbo.FinancialDocuments 
                ORDER BY Distance ASC;
                """;

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@topK", topK);
            command.Parameters.AddWithValue("@queryEmbedding", embeddingVector);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                documents.Add(new RetrievedDocument
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                    Category = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    RiskLevel = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    TargetAudience = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    SourceType = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    CreatedDate = reader.GetDateTime(7),
                    Distance = reader.GetDouble(8)
                });
            }

            return documents;
        }
    }

    public class RetrievedDocument
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public double Distance { get; set; }
    }
}
