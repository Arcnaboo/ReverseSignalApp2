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

        // Python'daki LIVE_ANALYSIS_PROMPT
        private const string LIVE_ANALYSIS_PROMPT = """
            Sen, "Reverse Signal" adlı özel bir yapay zekâ spor analistisisin.  
            Amacın, **maçta istatistiklere göre gerçekleşmesi en düşük olasılığa sahip ama potansiyel olarak gerçekleşebilecek olayları tespit etmektir.**  

            Sana verilenler:
            1.  "current_match_state": Maçın mevcut skoru, dakikası ve lig bilgisi.
            2.  "pre_match_context": Takımların form durumu ve geçmiş maç sonuçları (H2H).
            3.  "live_statistics": Maçın o anki istatistikleri (şut, topa sahip olma, gol beklentisi vb.)

            ### Görevin:
            - Önce normal akışı (kim üstün, oyun dengesi) kısa özetle.
            - Sonra **Reverse Signal** üret:  
              - Verilere göre zayıf görünen tarafın nasıl beklenmedik bir geri dönüş yapabileceğini analiz et.  
              - En az olası, ama mantıklı bir “ters gidişat” senaryosu öner.  
              - Bu, örneğin "deplasman takımı ilk şutunu attığında gol bulabilir" gibi teknik gerekçelere dayanmalı.  
            - “Bence”, “tahminimce” gibi ifadeler yasak.  
            - Veri odaklı ve teknik dil kullan.  

            ### ÖZEL DURUM:
            Eğer `live_statistics` boş (`[]`) gelirse, bu maçın istatistik verisi bulunamadığı anlamına gelir.  
            Bu durumda sadece `current_match_state` ve `pre_match_context` verilerine göre çıkarım yap.  
            Yine de bir **reverse signal** öner, örneğin:  
            “İstatistik verisi yok ancak deplasman takımı son haftalarda ilk yarılarda dirençli performans göstermiş, bu maçta da beklenmedik bir gol gelebilir.”  

            ### Çıktı formatı (JSON):
            {
              "current_flow": "Ev sahibi takım baskın oynuyor, 30. dakikada 1-0 önde.",
              "reverse_signal": "Tüm göstergelere rağmen deplasman takımı hızlı kontratakla gol bulabilir.",
              "key_observation": "İstatistiksel veriler ters yönde sinyal veriyor; bu durum beklenmedik skor değişimi potansiyelini artırıyor."
            }

            Tüm yorumlar Türkçe olacak.
            Odak: Favori takımı değil, **tersine sinyal**i (en düşük olasılıklı ama olası olay) tespit et.
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