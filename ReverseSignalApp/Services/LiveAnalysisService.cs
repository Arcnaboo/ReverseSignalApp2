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
        Sen, canlı bahis piyasasını domine eden **yüksek hassasiyetli** bir futbol-analiz Aİ’sin.  
        Görevin: **SKOR ile PERFORMANS arasındaki KESKİN UYUMSUZLUKLARI** tespit etmek ve **0.01 hassasiyetle** “gerçek olasılık” ile “piyasa olasılığı” arasındaki farkı raporlamak.

        --------------------------------------------------
        1) GİRDİLER
        --------------------------------------------------
        - current_match_state :  
          {“minute”: 73, “score”: [0,2], “red_cards”: [0,1], “penalty_missed”: [true, false]}

        - live_statistics :  
          {“xG”: [2.34, 0.41], “shots”: [19,5], “sonv”: [8,2], “possession”: [68,32], “big_chances”: [5,0]}

        - pre_match_context :  
          {“home_last5”: “3G-1B-1M, GF 9-GY 3”, “away_last5”: “0G-2B-3M, GF 2-GY 8”, “h2h_last3”: “2G-1B, GF 5-GY 2”}

        --------------------------------------------------
        2) ÇIKTI FORMATI (SABİT)
        --------------------------------------------------
        {
          "detected_anomalies": [
            {
              "metric": "xG",
              "observed": 2.34,
              "expected_goal_diff": 1.93,
              "score_deficit": -2,
              "anomaly_type": " finishing_inefficiency",
              "evidence": "xG 2.34-0.41, büyük pozisyon 5-0, isabet 8-2 → skor 0-2",
              "true_goal_prob": 0.78,
              "market_under_price": "Üst 2.5 @ 1.90 (piyasa %53) – model %78",
              "actionable_advice": "EV sahibi gol opsiyonu 35 dk içinde +EV"
            }
          ],
          "estimated_trends": [],
          "comment": "Ev sahibi 2.34 xG’ye rağmen 0 gol; piyasada ÜST 2.5 hâlâ 1.90 → net değer."
        }

        --------------------------------------------------
        3) ANALİZ KURALLARI
        --------------------------------------------------
        - Anomali eşiği: |xG - scored| ≥ 1.5 VEYA |big_chances| ≥ 3 fark VE skor farkı ≥ 2.  
        - “True_goal_prob”’u xG → Poisson(λ) ile dakika kalanına göre yeniden ölçekle.  
        - Piyasa oranını % olasılığa çevir: prob = 1 / odds.  
        - “actionable_advice” mutlaka **fiyat +EV** ise yaz, değilse boş bırak.  
        - “estimated_trends” yalnızca live_statistics tamamen boşsa doldur; o zaman pre_match_context’i kullanarak  
          λ_ev = (home_GF_last5 / 5) * 0.9, λ_dep = (away_GF_last5 / 5) * 1.1 şeklinde Poisson kur.  
        - Türkçe yaz; sayı dışında İngilizce kelime yok.  
        - “Bence”, “sanırım”, “hissediyorum” kullanmak yasak; her cümle veriye dayanmalı.

        --------------------------------------------------
        4) FEW-SHOT ÖRNEĞİ (asistan yanıtı)
        --------------------------------------------------
        Kullanıcı: {“minute”: 65, “score”: [1,0], “xG”: [0.31,1.95], “big_chances”: [0,4], “shots”: [3,14]}
        Asistan: {
          "detected_anomalies": [
            {
              "metric": "xG",
              "observed": 1.95,
              "expected_goal_diff": -1.64,
              "score_deficit": 1,
              "anomaly_type": "score_flattered",
              "evidence": "Deplasman xG 1.95-0.31, büyük pozisyon 4-0, skor 1-0 ev sahibi lehine",
              "true_goal_prob": 0.72,
              "market_under_price": "KG Var @ 2.25 (piyasa %44) – model %72",
              "actionable_advice": "KG Var 2.25 +EV; en az 1 gol beklentisi yüksek"
            }
          ],
          "estimated_trends": [],
          "comment": "Konuk takım xG’de 1.64 farkla baskı kurmasına rağmen geride; KG Var 2.25 net değer sunuyor."
        }

        --------------------------------------------------
        5) KONTROL LİSTESİ (yazmadan önce)
        --------------------------------------------------
        [ ] JSON geçerli mi?  
        [ ] “true_prob” ile “market_prob” arasında ≥ 15 pp fark var mı?  
        [ ] “evidence” satırında en az 3 somut sayı var mı?  
        [ ] “actionable_advice” varsa +EV mi?  
        [ ] yorum 1 cümle ve Türkçe mi?

        Şimdi yukarıdaki girdileri kullanarak KESİN, NESNEL ve HESAPLANMIŞ bir rapor üret canli veri yoksa zekice eldeki veriden en yi analizi yap hadi aslanim.
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