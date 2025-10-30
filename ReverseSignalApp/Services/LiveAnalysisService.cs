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
        private const string GROQ_API_KEY = "GkKr>^4l~ZnnVAG^zl$DEeZ+>2,ged~nl4X^z6|:VLE[=ezoe,B$w-06";
        private const string GROQ_API_URL = "https://api.groq.com/openai/v1/chat/completions";

        private const string LIVE_ANALYSIS_PROMPT = """
            Sen, canlı futbol maçlarını izleyen ve **piyasadaki olasılık hatalarını tespit eden** bir yapay zekâ analistisisin.

            Sana gönderilenler:
            - “current_match_state”: maçın dakika, skor, lig bilgisi
            - “pre_match_context”: takımların son 10 maçlık formları ve H2H geçmişi
            - “live_statistics”: şut, xG, topa sahip olma, kartlar gibi canlı istatistikler

            Görevin:
            - Maçın gidişatını **istatistiksel olarak** değerlendir. Kim üstün, momentum kimde?
            - Piyasada **çok düşük olasılık verilen ama veriye göre gerçekleşme potansiyeli taşıyan** olayları belirle.
            - Bu olayları “reverse_signals” listesinde topla.

            Değerlendirmede:
            - xG farkı, şut sayısı, son 15 dakikadaki tempo değişimi, pas yüzdesi, kart riski, yorgunluk, form grafiği gibi faktörleri analiz et.
            - Örnek: “Ev sahibi 70’ten sonra gol atamaz deniyor (%10), ama xG’si 1.8 ve baskısı artıyor, gol bulabilir.”

            Çıktı şu JSON formatında olmalı:
            {
              "reverse_signals": [
                {
                  "scenario": "Deplasman Takımı Gol Bulur @ 8.00 (%12)",
                  "true_prob": "%30,4",
                  "evidence": "Ev sahibi topa %68 sahip ama son 10 dakikada şut yok, deplasman kontra fırsatları artıyor."
                }
              ],
              "summary": "Ev sahibi baskın ama yoruluyor; deplasman gol potansiyeli artıyor."
            }

            Notlar:
            - Tüm yorumlar Türkçe olacak.
            - "reverse_signals" listesi boşsa bile en az bir mantıklı ters olasılık üret.
            - JSON dışında hiçbir şey yazma.
            """;



        // Groq için statik HttpClient
        private static readonly HttpClient _httpClient;

        static LiveAnalysisService()
        {
            var testGuid = new Guid("2b150884-be96-4854-85b8-d7e63101ca46");
            var eservis = new Enigma3Service();
            var apikey = eservis.Decrypt(testGuid, GROQ_API_KEY);


            _httpClient = new HttpClient();
           
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apikey);
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