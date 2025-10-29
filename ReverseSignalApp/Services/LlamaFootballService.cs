using ReverseSignalApp.Services; // Modelleri (Adım 1) kullanmak için
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers; // Header'ları eklemek için

namespace ReverseSignalApp.Services
{
    // Python'daki LlamaFootballService sınıfına karşılık gelir.
    public class LlamaFootballService
    {
        // Python'daki GROQ sabitleri
        private const string GROQ_API_KEY = "GkKr>^4l~ZnnVAG^zl$DEeZ+>2,ged~nl4X^z6|:VLE[=ezoe,B$w-06";
        private const string GROQ_API_URL = "https://api.groq.com/openai/v1/chat/completions";

        // Python'daki IMPOSSIBLE_ODDS_PROMPT
        private const string IMPOSSIBLE_ODDS_PROMPT = """
        Sen, bahis piyasasındaki hataları tespit eden bir yapay-zekâ analistisisin.

        Sana gönderilenler:
        - Gelecekteki TEK bir maç (“odak” maçı)
        - Ev sahibi takımın son 10 TAMAMLANMIŞ maçı
        - Deplasman takımın son 10 TAMAMLANMIŞ maçı
        - Tarafların son 6 karşılaşmasından oluşan birbirine karşı H2H geçmişi

        Görevin:
        - Piyasada ~%10 olasılık verilen ama senin modeline göre <%1 (veya tersi) sonuçları belirle.
        - xG (gol beklentisi) trendleri, gol ortalamaları, temiz sayı serileri, BTTS (karşılıklı gol) fiyatları, takımların hafta içi / deplasman performansı gibi ipuçlarını kullan.
        - Oran hatalarını “impossible_odds” listesine koy.

        Çıktı şu JSON formatında olmalı:
        {
          "impossible_odds": [
            {
              "market": "Fulham Kazanır @ 2,50 (%40)",
              "true_prob": "%25,1",
              "evidence": "Fulham evde ort. 1,2 gol, Wolves deplasman ort. 1,1 gol, son 5 iç sahadan 3-1-1"
            }
          ],
          "comments": "Tek cümlelik yorum (Türkçe)."
        }

        Yorumların tamamı Türkçe olacak, sayısal değerler dışında İngilizce kelime kullanma.
        """;

        // Groq için statik HttpClient (Python'daki modül seviyesi HEADERS gibi)
        private static readonly HttpClient _httpClient;

        // Static constructor, HttpClient'ı bir kez ayarlar
        public static LlamaFootballService Instance { get; private set; } = new LlamaFootballService();
        static LlamaFootballService()
        {
            var testGuid = new Guid("2b150884-be96-4854-85b8-d7e63101ca46");
            var eservis = new Enigma3Service();
            var apikey = eservis.Decrypt(testGuid, GROQ_API_KEY);
            _httpClient = new HttpClient();
            // BaseAddress'i burada ayarlamıyoruz, çünkü PostAsync'te tam URL veriyoruz.
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apikey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // Python'daki __init__
        public LlamaFootballService()
        {
            Console.WriteLine("✅ LlamaFootballService initialized (Groq LLaMA model)");

        }

        // Python'daki analyze_impossible_odds
        // Kural 1: Tamamen async
        public async Task<string> AnalyzeImpossibleOddsAsync(Dictionary<string, object> context)
        {
            // Python'daki 'messages' ve 'payload'
            var payload = new
            {
                model = "llama-3.1-70b-versatile", // Python'da 3.3 kullanıyordun, 3.1 daha hızlı olabilir
                messages = new[]
                {
                    new { role = "system", content = IMPOSSIBLE_ODDS_PROMPT },
                    new { role = "user", content = JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = false }) }
                },
                temperature = 0.2
            };

            // Python'daki requests.post(url, headers=HEADERS, json=payload)
            try
            {
                var jsonPayload = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(GROQ_API_URL, httpContent);

                response.EnsureSuccessStatusCode(); // Hata varsa exception fırlat

                var jsonResponse = await response.Content.ReadAsStringAsync();

                // Python'daki result["choices"][0]["message"]["content"]
                using (var jsonDoc = JsonDocument.Parse(jsonResponse))
                {
                    var resultText = jsonDoc.RootElement
                                        .GetProperty("choices")[0]
                                        .GetProperty("message")
                                        .GetProperty("content")
                                        .GetString();

                    return resultText ?? "{\"error\": \"LLaMA'dan boş yanıt geldi\"}";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"❌ Groq API Error: {e.Message}");
                return $"{{\"error\": \"Groq API çağrılırken hata oluştu: {e.Message}\"}}";
            }
        }
    }
}