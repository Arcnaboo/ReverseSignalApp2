using ReverseSignalApp.Services; // Modelleri (Adım 1) kullanmak için
using System.Net.Http.Headers; // Header'ları eklemek için
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReverseSignalApp.Services
{
    // Python'daki LlamaFootballService sınıfına karşılık gelir.
    public class LlamaFootballService
    {
        // ... (GROQ_API_KEY, GROQ_API_URL, IMPOSSIBLE_ODDS_PROMPT ve HttpClient/Instance tanımları aynı kalır)

        private const string GROQ_API_KEY = "GkKr%{uGI~o?TKfDVf`_MB-L>2,ged~noGyk%{e&0$;Gl0V[DH<pXE<&";
        private const string GROQ_API_URL = "https://api.groq.com/openai/v1/chat/completions";

        // Python'daki IMPOSSIBLE_ODDS_PROMPT
        private const string IMPOSSIBLE_ODDS_PROMPT = """
Sen, bahis piyasasındaki olasılık hatalarını tespit eden uzman bir yapay zekâ analistisisin.  
Görevin: geçmiş form, istatistiksel eğilimler ve piyasa oranlarını kıyaslayarak “oran manipülasyonu” veya “istatistiksel tutarsızlık” içeren sonuçları belirlemektir.  
Cevabını yalnızca belirtilen JSON formatında ver.

GİRDİLER:
- "focus_match": Tahmin yapılacak tek maç (ör: "Team A vs Team B")
- "home_last10": Ev sahibi takımın son 10 tamamlanmış maçı (skor, xG, iç/deplasman bilgisi dahil)
- "away_last10": Deplasman takımının son 10 tamamlanmış maçı
- "h2h_last6": İki takım arasındaki son 6 karşılaşmanın geçmişi
- "market": { "market_name": string, "market_odds": string, "market_implied_prob": float } // örn. "Üst 2.5 @ 1.90 (%52)"

GÖREV:
- Geçmiş 10 maçtaki gol ortalamaları, xG trendleri, BTTS (karşılıklı gol), ve H2H istatistiklerini analiz et.  
- Piyasada ~%10–20 olasılık verilen ama modeline göre <%5 (veya tersi) görünen sonuçları tespit et.  
- “Son 5 maç düşük skorlu” (ör. 1-0, 0-0, 1-1) olup da piyasanın “üst” fiyatladığı veya tam tersi “üst trendli” olup “alt” fiyatladığı durumları özellikle kontrol et.  
- Anomali eşiği: market_implied_prob / true_prob ≥ 2.0 veya ≤ 0.5 → oran hatası olarak raporla.  
- Form, yorgunluk, hafta içi yoğunluğu, kırmızı kart geçmişi, deplasman-ev farkı gibi bağlamsal etkenleri kanıt cümlesine dahil et.

DEĞERLENDİRME KRİTERLERİ:
1. Son 5–10 maçta toplam gol ortalaması (avg_goals_home / avg_goals_away) ile piyasanın “Üst/Alt” fiyatlaması uyuşmuyorsa uyarı üret.  
2. xG trendinde belirgin bir sapma (ör. son 3 maçta xG ortalaması 2.1 → 0.8’e düşüş) varsa bu konsantrasyon/performans düşüşü sinyalidir.  
3. Takımların son 6 H2H maçında gol eğilimi piyasa beklentisiyle çelişiyorsa bunu “evidence” kısmına yaz.  
4. Sadece istatistiksel ve rasyonel anormallikleri listele — spekülatif yorum yapma.  
5. Tüm metinler Türkçe olacak; sayısal değerler hariç İngilizce kelime kullanılmayacak.  
6. Yüzde değerler tek ondalıkla yazılacak (ör. "%8.3").

ÇIKTI JSON FORMAT:
{
  "impossible_odds": [
    {
      "market": "Üst 2.5 Gol @ 1.90 (%52)",
      "true_prob": "%23.7",
      "edge_ratio": 2.19,
      "evidence": "Son 5 maçta toplam gol ort. 1.4; iki takımın 4 H2H maçı alt bitti; piyasa %52 fiyatlamış ama veri %24 gösteriyor.",
      "signals": ["low_goal_trend", "market_overpriced", "h2h_under_pattern"],
      "confidence": "yüksek"
    },
    {
      "market": "Team B Kazanır @ 4.80 (%20)",
      "true_prob": "%9.5",
      "edge_ratio": 2.1,
      "evidence": "Deplasman son 8 maçta 1 galibiyet; son 4 dış saha maçında xG ort. 0.7; buna rağmen piyasa %20 olasılık veriyor.",
      "signals": ["away_form_decline", "low_xg_away", "market_bias"],
      "confidence": "orta"
    }
  ],
  "comments": "Son 10 maç verileri düşük skoru işaret ederken piyasa yüksek tempolu maç bekliyor — oranlarda aşırı iyimserlik var."
}
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
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apikey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public LlamaFootballService()
        {
            Console.WriteLine("✅ LlamaFootballService initialized (Groq LLaMA model)");
        }

        // Python'daki analyze_impossible_odds
        public async Task<string> AnalyzeImpossibleOddsAsync(Dictionary<string, object> context)
        {
            // Python'daki json.dumps() işlevini doğru şekilde taklit etmek için seçenekler.
            // 1. PropertyNamingPolicy = CamelCase: C# PascalCase (HomeTeam) -> JSON camelCase (homeTeam) yapar.
            //    Bu, 400 Bad Request hatasını çözmelidir.
            // 2. Converters: DateTime'ların doğru formatta gönderilmesini sağlar (Python'daki default=str'ın bir karşılığı).
            var contextSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                // DateTime'ların ISO formatında doğru serileşmesini sağlar.
                Converters = { new JsonStringEnumConverter() }
            };

            // Context objesini önce doğru seçeneklerle serileştiriyoruz.
            var contextJsonString = JsonSerializer.Serialize(context, contextSerializerOptions);

            // Ana payload için de aynı seçenekleri kullanıyoruz.
            var payloadSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };


            // Python'daki 'messages' ve 'payload'
            var payload = new
            {
                // Python'da 3.3 kullanıyordunuz, C# kodunu 3.3 olarak güncelliyorum.
                model = "llama-3.3-70b-versatile",
                messages = new[]
                {
                    new { role = "system", content = IMPOSSIBLE_ODDS_PROMPT },
                    // Context'i string olarak ekliyoruz (Python'daki gibi).
                    new { role = "user", content = contextJsonString }
                },
                temperature = 0.2
            };

            try
            {
                // Payload'u son kez JSON string'ine çeviriyoruz.
                var finalJsonPayload = JsonSerializer.Serialize(payload, payloadSerializerOptions);
                var httpContent = new StringContent(finalJsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(GROQ_API_URL, httpContent);

                response.EnsureSuccessStatusCode();

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