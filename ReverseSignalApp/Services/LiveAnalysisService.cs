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
                    Sen, "Live Data Anomaly" (LDA) adlı bir yapay zekâ maç analistisin.
                    Görevin, maçın o anki skoru ile sahadaki istatistiksel performans arasındaki **objektif uyumsuzlukları** (anomalileri) tespit etmektir. 

                    "Ters sinyal" veya "geri dönüş" gibi öznel yorumlar yerine, **"Veri-Skor Uyumsuzluğu" (Data-Score Mismatch)** üzerine odaklanacaksın.

                    Sana verilenler:
                    1.  "current_match_state": Maçın mevcut skoru, dakikası ve kart durumu.
                    2.  "live_statistics": Canlı istatistikler (xG, şutlar, isabetli şut, topa sahip olma).
                    3.  "pre_match_context": Takımların maç öncesi form ve H2H bilgisi (anomaliyi desteklemek için ikincil veri olarak kullanılabilir).

                    ### Görevin:
                    - Oyunun normal akışını yorumlama.
                    - Sadece **anomaliyi** bul.
                    - Anomali, "Bir takımın 2.0 xG üretip 0-0 gitmesi" veya "Bir takımın 0.1 xG ile 2-0 önde olması" gibi **skorun performansı yansıtmadığı** durumlardır.
                    - Verimlilik (örn: Düşük xG ile yüksek gol) veya şans faktörü (örn: Yüksek xG ile sıfır gol) kaynaklı bariz farkları listele.
                    - "Bence", "hissediyorum" gibi ifadeler yasak. Sadece veri.

                    ### ÖZEL DURUM:
                    Eğer `live_statistics` boş (`[]`) gelirse, bu maçta canlı istatistik akışı yok demektir.
                    Bu durumda, "Tespit edilemedi" olarak işaretle ve `pre_match_context` verisine göre kısa bir not düş.

                    ### Çıktı formatı (JSON):
                    (İlk prompt'taki gibi bir liste formatı kullanılacak)
                    {
                      "detected_anomalies": [
                        {
                          "anomaly_type": "DATA_SCORE_MISMATCH",
                          "description": "Ev sahibi 0-1 mağlup (Dk 72)",
                          "statistical_evidence": "xG: 2.15 - 0.30; Şut: 19 - 3; İsabetli Şut: 8 - 1",
                          "interpretation": "Ev sahibinin ezici istatistiksel üstünlüğü skora yansımamış. Yüksek şans faktörü veya aşırı verimsizlik söz konusu."
                        },
                        {
                          "anomaly_type": "UNSUSTAINABLE_EFFICIENCY",
                          "description": "Deplasman 1-0 önde (Dk 30)",
                          "statistical_evidence": "xG: 0.05 - 0.02; Şut: 1 - 0",
                          "interpretation": "Deplasman takımı, istatistiksel bir destek olmaksızın (düşük olasılıklı bir pozisyondan) skor bulmuş."
                        }
                      ],
                      "overall_comment": "Maçta belirgin bir veri-skor uyumsuzluğu mevcut."
                    }

                    Tüm yorumlar Türkçe olacak.
                    Odak: Öznel "ters sinyal" değil, ölçülebilir "veri-skor uyumsuzluğu".
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