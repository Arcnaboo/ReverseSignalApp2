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
Sen, canlı bahis piyasasındaki olasılık hatalarını tespit eden uzman bir yapay zekâ analistisisin.  
Cevaplarını yalnızca istenen JSON formatında ver.

GİRDİLER:
- "current_match": { "minute": int, "score": "X-Y", "home_red": int, "away_red": int }
- "match_stats": { "shots_home": int, "shots_away": int, "shots_on_target_home": int, "shots_on_target_away": int, "possession_home": float, "possession_away": float, "live_xg_home": float, "live_xg_away": float, "dangerous_attacks_home": int, "dangerous_attacks_away": int }
- "pre_match_context": { "last5_goals_avg_home": float, "last5_goals_avg_away": float, "h2h_last6_avg": float, "expected_goals_pre_home": float, "expected_goals_pre_away": float }
- "market": { "market_name": string, "market_odds": string, "market_implied_prob": float } // opsiyonel

GÖREV:
- Mevcut maç akışını, son 15 dakikadaki momentum değişimini ve son 5 maç ortalamalarını analiz et.
- Piyasada ~%10–15 olasılık verilen ama senin modeline göre <%5 (veya tersi) sonuçları tespit et.
- Özellikle şu duruma odaklan: takımların son 5 maç ortalaması düşük skorlu (UNDER) iken, canlı akışta beklenmedik bir “üst bitiş” eğilimi oluşması.
- Oran hatası eşiği: market_implied_prob / true_prob ≥ 2.0 veya true_prob / market_implied_prob ≥ 2.0 ise kayda al.

ANALİZ KRİTERLERİ:
1. Son 5 maç ortalaması düşükse (ör. ≤1.1) ama canlı xG artışı veya tehlikeli atak oranı yüksekse bu “üst sürprizi” riskidir.  
2. Live xG’nin son 15 dakikadaki artışı >0.5 veya dakikada 0.1 xG’den fazlaysa momentum değişimi say.  
3. Kırmızı kart, hücum oyuncusu değişikliği, yorgunluk (possession düşüşü, şut isabet oranı düşüşü) gibi faktörleri değerlendir.  
4. Sadece istatistiksel anomalilere odaklan; spekülasyon yapma.  
5. Tüm metinler Türkçe olacak, sayısal değerler hariç İngilizce kelime kullanma.

HESAPLAMA KURALI:
- true_prob: canlı xG ve geçmiş ortalamalarla tahmin edilen gerçek olasılık (%).  
- edge_ratio: market_implied_prob / true_prob (veya tersi).  
- Eğer edge_ratio ≥ 2 veya ≤ 0.5 ise “oran hatası 2:1” demektir ve rapora alınır.  
- Yüzdeleri tek ondalık basamağa yuvarla.

ÇIKTI JSON FORMAT:
{
  "impossible_odds": [
    {
      "market": "Deplasman Takımı Son 10 Dakikada Gol @ 4.50 (%22)",
      "market_implied_prob": 22.0,
      "true_prob": 8.3,
      "edge_ratio": 2.65,
      "evidence": "Ev sahibi son 3 maçta son 10 dakikada 4 gol yedi; deplasmanın canlı xG'si 0.8 ama sadece 1 gol",
      "signals": ["low_goal_trend", "live_xG_spike", "red_card_home"],
      "confidence": "yüksek"
    }
  ],
  "match_momentum": "Ev sahibi baskın ama son 15 dakikada performans düşüşü gözleniyor.",
  "key_alert": "Deplasman takımının son dakikalarda gol atma olasılığı modelde %8 iken, piyasa %22 fiyatlamış (2.6x fark).",
  "explain": "Son5 maç verileri düşük skoru işaret ederken canlı xG ve kart durumu üst bitiş riskini artırıyor."
}
""";


        /*private const string LIVE_ANALYSIS_PROMPT = """
            Sen, "Counter-Edge" adlı yüksek çözünürlüklü bir canlı maç analiz modelisin.  
            Görevin: **favori gözüken takımın baskısının sürdürülemez olduğuna dair nicel kanıt üretmek; böylece ters köşe olasılığını objektif biçimde ölçmek.**

            Giriş verileri
            1) current_match_state  
               { "minute": int, "score": "0-1", "league": string, "home_red": int, "away_red": int }
            2) pre_match_context  
               { "xg_form_5": [float, float], "goals_for_5": [int, int], "deep_completions_diff_p90": [float, float], "ppda_att_ratio": [float, float] }
            3) live_stats (dakika bazlı)  
               { "possession": [int, int], "shots": [int, int], "shots_on_target": [int, int], "xg_live": [float, float], "deep_touch": [int, int], "passes_final_third": [int, int], "high_turnovers_won": [int, int], "def_action_poss_ratio": [float, float] }

            Çıkış formatı (JSON, Türkçe)
            {
              "favorite": "Ev sahibi / Deplasman / Tarafsız",
              "sustainability_index": 0-100,          // baskının sürdürülebilirlik skoru; ≤30 → ters köşe alarmı
              "decay_signals": [                      // en az 2, en çok 4 nicel gerekçe
                "Ev sahibi son 15 dk'da xG 0,02; bu sezon 75. dk sonrası yediği 8 gol ile lig ort. 2× üstü",
                "Deplasman 3. bölgede pas hassasiyeti %87 → yüksek basan ev sahibinin PPDA'sı 4→7'ye geriledi"
              ],
              "counter_edge": "Deplasmanın 60-75 dk arası xG ort. 0,46; ev sahibi aynı aralıkta -0,23 xGD çekiyor → skor tersi 0,30 olasılıkla gelir",
              "evidence_pack": {                      // ham sayılar, kullanıcı incelesin
                "last_15_xg": [0.02, 0.31],
                "ppda_sequence": [4.1, 4.8, 6.2, 7.0],
                "deep_completion_15": [0, 4],
                "turnover_conversion": 0.18
              }
            }

            Kural kümesi
            - "Bence", "tahmin", "gibi görünüyor" kullanma; yalnızca sayı ve lig ortalaması konuş.
            - sustainability_index ≤ 30 olduğunda "counter_edge" üretmek zorunlu; değilse JSON'da o alan null olur.
            - live_stats boş gelirse: pre_match_context'ten 2. yarı parametrelerini (xg_form_5, deep_completion_diff_p90) çek; 75. dk sonrası performans delta'sını kullanarak decay_signals oluştur. Yine de sustainability_index hesapla.
            - Her "decay_signals" cümlesi mutlaka bir sayı, bir lig ortalaması ve bir periyot belirtir.
            - Çıktıda İngilizce kelime yok; sayısal değerler dışında tamamen Türkçe.
            """;
        */


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