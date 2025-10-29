using ReverseSignalApp.Services; // Modelleri (Adım 1) kullanmak için
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;

namespace ReverseSignalApp.Services
{
    // Python'daki LiveAnalysisService sınıfına karşılık gelir.
    public class LiveAnalysisService
    {
        // Python'daki GROQ sabitleri
        private const string GROQ_API_KEY = "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";
        private const string GROQ_API_URL = "https://api.groq.com/openai/v1/chat/completions";

        // Python'daki LIVE_ANALYSIS_PROMPT
        private const string LIVE_ANALYSIS_PROMPT = """
        Sen, canlı maçları analiz eden ve gidişatı yorumlayan bir yapay-zekâ spor analistisisin.

        Sana gönderilenler:
        1.  "current_match_state": Maçın mevcut skoru, dakikası ve maçın hangi ligde olduğu.
        2.  "pre_match_context": Takımların maç öncesi form durumları ve H2H geçmişi.
        3.  "live_statistics": Maçın o anki canlı istatistikleri.

        Görevin:
        - Maçın mevcut gidişatını (flow) teknik bir dille özetle.
        - Canlı istatistiklere bakarak hangi takımın daha baskın olduğunu belirt.
        - Mevcut skora ve istatistiklere bakarak bir sonraki golün kime daha yakın olduğunu (örn: maçın 2-0 mı yoksa 1-1 mi olmaya daha yakın olduğunu) analiz et.
        - "Bence", "tahminimce" gibi öznel ifadeler kullanma. Sadece verilere dayanarak teknik bir yorum yap.

        **ÖNEMLİ KURAL:**
        Eğer `live_statistics` listesi boş (`[]`) gelirse, bu, maç için detaylı istatistik (şut, posesyon) olmadığı anlamına gelir. Bu durumda, analizini SADECE `current_match_state` (mevcut skor, dakika) ve `pre_match_context` (maç öncesi form) verilerine göre yap. `current_flow` ve `next_goal_prediction` alanlarını bu kısıtlı veriye göre doldur. `key_observation` alanında "Detaylı canlı istatistik verisi bulunmuyor." yaz.

        Çıktı şu JSON formatında olmalı (İstatistik yoksa örnek):
        {
          "current_flow": "Ev sahibi takım, 25. dakikada 1-0 önde. Maçın detaylı canlı istatistikleri mevcut değil.",
          "next_goal_prediction": "İstatistik verisi olmamasına rağmen, ev sahibi takımın maç öncesi formu (son 5 maç 4G) göz önüne alındığında, skoru korumaya yakın.",
          "key_observation": "Detaylı canlı istatistik verisi bulunmuyor."
        }

        Tüm yorumlar Türkçe olacak.
        Her zaman tahmin sonucunda hangi takımın lehine skorun değişmesi muhtemel olduğunu belirt.
        Örneğin: 'Ev sahibi gol atabilir' veya 'Deplasman takımı gol bulabilir.'
        """;

        // Groq için statik HttpClient
        private static readonly HttpClient _httpClient;

        static LiveAnalysisService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GROQ_API_KEY);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public static LiveAnalysisService Instance { get; private set; } = new LiveAnalysisService();
        // Python'daki __init__
        public LiveAnalysisService()
        {
            Console.WriteLine("✅ LiveAnalysisService initialized (Groq LLaMA model)");
        }

        // Python'daki analyze_live_match
        // Kural 1: Tamamen async
        // Not: C# modelini (TeamStatisticWrapper) kullanıyoruz, Dict değil.
        public async Task<string> AnalyzeLiveMatchAsync(
            MatchModel focal_match,
            Dictionary<string, object> pre_match_context,
            List<TeamStatisticWrapper> live_stats)
        {
            Console.WriteLine("🧠  LLaMA'ya canlı analiz için veri gönderiliyor...");

            // Python'daki 'combined_input'
            var combined_input = new
            {
                current_match_state = focal_match, // C# record'u otomatik serialize olur
                pre_match_context = pre_match_context,
                live_statistics = live_stats // C# listesi otomatik serialize olur
            };

            // Python'daki 'payload'
            var payload = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "system", content = LIVE_ANALYSIS_PROMPT },
                    new { role = "user", content = JsonSerializer.Serialize(combined_input, new JsonSerializerOptions { WriteIndented = false }) }
                },
                temperature = 0.3
            };

            try
            {
                var jsonPayload = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(GROQ_API_URL, httpContent);

                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                using (var jsonDoc = JsonDocument.Parse(jsonResponse))
                {
                    var resultText = jsonDoc.RootElement
                                        .GetProperty("choices")[0]
                                        .GetProperty("message")
                                        .GetProperty("content")
                                        .GetString();

                    if (string.IsNullOrEmpty(resultText))
                    {
                        return "{\"error\": \"LLaMA'dan boş yanıt geldi\"}";
                    }

                    // Python'daki ```json ... ``` temizliği
                    if (resultText.StartsWith("```json"))
                    {
                        resultText = resultText.Substring(7, resultText.Length - 10).Trim();
                    }

                    return resultText;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"❌ Groq API Hatası: {e.Message}");
                return $"{{\"error\": \"Groq API çağrılırken hata oluştu: {e.Message}\"}}";
            }
        }
    }
}