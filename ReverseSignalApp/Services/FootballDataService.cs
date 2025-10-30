using ReverseSignalApp.Services; // Modelleri (Adım 1) kullanmak için
using System.Text.Json;
using System.Net.Http.Headers; // Header'ları eklemek için

// Senin referans verdiğin doğru namespace
namespace ReverseSignalApp.Services
{
    // Python'daki APIFootballService sınıfına karşılık gelir.
    public class FootballDataService
    {
        // Python'daki API_KEY ve BASE_URL sabitleri
        private const string API_KEY = "^<d77T-7udV<d.7-\\e,\\\\E^7TE-d7-\\,";
        private const string BASE_URL = "https://v3.football.api-sports.io";

        // Python'daki gibi, servis kendi HttpClient'ını yönetir.
        // 'static readonly' kullanmak, socket tükenmesini önleyen en iyi C# yöntemidir.
        private static readonly HttpClient _httpClient;

        public static FootballDataService Instance { get; private set; } = new FootballDataService();

        // Static constructor, HttpClient'ı bir kez ayarlar
        static FootballDataService()
        {
            var testGuid = new Guid("2b150884-be96-4854-85b8-d7e63101ca46");
            var eservis = new Enigma3Service();
            var apikey = eservis.Decrypt(testGuid, API_KEY);
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BASE_URL);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-apisports-key", apikey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // Python'daki __init__'e karşılık gelir.
        public FootballDataService()
        {
            // Python'daki gibi, 'init' mesajı.
            Console.WriteLine("✅ API-Football Service initialized (Singleton)");
        }

        // ----------------------------------------------------------

        // ----------------------------------------------------------
        public async Task<List<MatchModel>> GetFixturesAsync(int? league_id = null, string? from_date = null, string? to_date = null, string? status = null, int? team_id = null, string? last = null, string? h2h = null, int? season = null)
        {
            var parameters = new Dictionary<string, string>();
            if (league_id.HasValue) parameters.Add("league", league_id.Value.ToString());
            if (season.HasValue) parameters.Add("season", season.Value.ToString());
            if (from_date != null) parameters.Add("from", from_date);
            if (to_date != null) parameters.Add("to", to_date);
            if (status != null) parameters.Add("status", status);
            if (team_id.HasValue) parameters.Add("team", team_id.Value.ToString());
            if (last != null) parameters.Add("last", last);
            if (h2h != null) parameters.Add("h2h", h2h);

            var apiResponse = await GetAndDeserializeAsync<ApiFootballResponse>("/fixtures", parameters);

            if (apiResponse?.Response == null)
            {
                return new List<MatchModel>();
            }

            // Python'daki _parse_matches_v3 mantığı
            var matchModels = apiResponse.Response.Select(ParseFixtureWrapper).ToList();

            // Console.WriteLine($"✅ Fetched {matchModels.Count} fixtures."); // (Python'da bu satır yorumluydu)
            return matchModels;
        }

        // ----------------------------------------------------------
        // get_live_fixtures metodu (Python'daki)
        // ----------------------------------------------------------
        public async Task<List<MatchModel>> GetLiveFixturesAsync(int? league_id = null)
        {
            var parameters = new Dictionary<string, string> { { "timezone", "UTC" } };

            if (league_id.HasValue)
            {
                parameters.Add("live", league_id.Value.ToString());
                Console.WriteLine($"🔴  GET live fixtures for league={league_id}");
            }
            else
            {
                parameters.Add("live", "all");
                Console.WriteLine($"🔴  GET all live fixtures (live=all)");
            }

            var apiResponse = await GetAndDeserializeAsync<ApiFootballResponse>("/fixtures", parameters);

            if (apiResponse?.Response == null || !apiResponse.Response.Any())
            {
                Console.WriteLine("⚠️  No 'live=all' matches right now.");
                return new List<MatchModel>();
            }

            var live = apiResponse.Response.Select(ParseFixtureWrapper).ToList();
            Console.WriteLine($"🔴  {live.Count} live fixture candidates found.");
            return live;
        }

        // ----------------------------------------------------------
        // get_live_today metodu (Python'daki, Events kontrolü yapan)
        // ----------------------------------------------------------
        public async Task<List<MatchModel>> GetLiveTodayAsync(int? league_id = null, string? day = null, int max_min = 90)
        {
            // 1. Adayları al
            var candidates = await GetLiveFixturesAsync(league_id);
            if (!candidates.Any())
            {
                return new List<MatchModel>();
            }

            var live = new List<MatchModel>();
            var filter_date_str = day ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

            // 2. Events'ları kontrol et
            foreach (var m in candidates)
            {
                if (m.UtcDate.ToString("yyyy-MM-dd") != filter_date_str)
                {
                    continue;
                }

                try
                {
                    var eventsResponse = await GetEventsAsync(m.Id); // Bu metodu aşağıda ekledim
                    if (eventsResponse?.Response == null || !eventsResponse.Response.Any())
                    {
                        continue;
                    }

                    // Python'daki 'max(event["time"]["elapsed"])' mantığı
                    int last_elapsed = eventsResponse.Response
                        .Where(e => e.Time?.Elapsed.HasValue ?? false)
                        .Select(e => e.Time!.Elapsed!.Value)
                        .DefaultIfEmpty(0) // Liste boşsa 0 döndür
                        .Max();

                    if (last_elapsed > 0 && last_elapsed <= max_min)
                    {
                        m.Status = $"{last_elapsed}'"; // Durumu dakika ile güncelle
                        live.Add(m);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"⚠️  Event check failed for fixture {m.Id}: {e.Message}");
                    continue;
                }
            }

            Console.WriteLine($"🔴  Events’a göre canlı: {live.Count} maç");
            return live;
        }

        // ----------------------------------------------------------
        // get_live_statistics metodu (Python'daki)
        // ----------------------------------------------------------
        public async Task<List<TeamStatisticWrapper>> GetLiveStatisticsAsync(int fixtureId)
        {
            var parameters = new Dictionary<string, string> { { "fixture", fixtureId.ToString() } };
            Console.WriteLine($"📊  GET /fixtures/statistics  fixture_id={fixtureId}");

            var apiResponse = await GetAndDeserializeAsync<ApiStatisticsResponse>("/fixtures/statistics", parameters);

            if (apiResponse?.Response == null || !apiResponse.Response.Any())
            {
                Console.WriteLine("⚠️  Bu maç için canlı istatistik verisi (henüz) bulunamadı.");
                return new List<TeamStatisticWrapper>();
            }

            // Python'daki formatlama mantığı (C#'ta doğrudan wrapper'ı döndürmek daha temiz)
            return apiResponse.Response;
        }

        // ----------------------------------------------------------
        // build_focal_context metodu (Python'daki)
        // ----------------------------------------------------------
        public async Task<Dictionary<string, object>> BuildFocalContextAsync(MatchModel focal, int form_length = 10)
        {
            var season = focal.UtcDate.Year;
            var form_length_str = form_length.ToString();

            // Kural 1: GUI kilitlememek için görevleri paralel başlatıyoruz.
            var homeFormTask = GetFixturesAsync(
                team_id: focal.HomeTeam.Id,
                season: season,
                last: form_length_str,
                status: "FT"
            );

            var awayFormTask = GetFixturesAsync(
                team_id: focal.AwayTeam.Id,
                season: season,
                last: form_length_str,
                status: "FT"
            );

            var h2hTask = GetFixturesAsync(
                h2h: $"{focal.HomeTeam.Id}-{focal.AwayTeam.Id}",
                season: season,
                last: "6",
                status: "FT"
            );

            // Kural 1: Tüm asenkron görevlerin bitmesini bekliyoruz.
            await Task.WhenAll(homeFormTask, awayFormTask, h2hTask);

            // Görevlerin sonuçlarını alıyoruz
            var home_past = await homeFormTask;
            var away_past = await awayFormTask;
            var h2h = await h2hTask;

            // Python'daki dict'in C# karşılığı
            var context = new Dictionary<string, object>
            {
                { "focal", focal }, // (Python'da .dict() vardı, C#'ta gerek yok)
                { "home_form", home_past },
                { "away_form", away_past },
                { "h2h", h2h }
            };

            return context;
        }

        // ----------------------------------------------------------
        // Dahili (Private) Yardımcı Metotlar
        // ----------------------------------------------------------

        // Python'daki _parse_matches_v3'ün C# karşılığı
        private MatchModel ParseFixtureWrapper(FixtureWrapper wrapper)
        {
            // Python'daki TeamModel ve MatchScore'un C# record'larına dönüştürülmesi
            var homeTeam = new TeamModel(
                wrapper.Teams.Home.Id,
                wrapper.Teams.Home.Name,
                wrapper.Teams.Home.ShortName, // Bu alanlar JSON'da varsa gelir
                wrapper.Teams.Home.Tla,
                wrapper.Teams.Home.CrestUrl,
                wrapper.Teams.Home.Venue,
                wrapper.Teams.Home.Founded
            );

            var awayTeam = new TeamModel(
                wrapper.Teams.Away.Id,
                wrapper.Teams.Away.Name,
                wrapper.Teams.Away.ShortName,
                wrapper.Teams.Away.Tla,
                wrapper.Teams.Away.CrestUrl,
                wrapper.Teams.Away.Venue,
                wrapper.Teams.Away.Founded
            );

            var score = new MatchScore(wrapper.Goals.Home, wrapper.Goals.Away);

            return new MatchModel
            {
                Id = wrapper.Fixture.Id,
                UtcDate = wrapper.Fixture.Date,
                Status = wrapper.Fixture.Status.Short,
                Competition = wrapper.League.Name,
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                Score = score,
                Stage = wrapper.League.Round,
                Group = null, // Python'daki gibi
                LastUpdated = DateTime.UtcNow // Python'daki gibi
            };
        }

        // Python'da olmayan ama C#'ta gereken GetEventsAsync
        private async Task<ApiEventsResponse?> GetEventsAsync(int fixtureId)
        {
            var parameters = new Dictionary<string, string> { { "fixture", fixtureId.ToString() } };
            return await GetAndDeserializeAsync<ApiEventsResponse>("/fixtures/events", parameters);
        }

        // Python'daki requests.get() yerine kullandığımız genel yardımcı metot
        private async Task<T?> GetAndDeserializeAsync<T>(string endpoint, Dictionary<string, string>? parameters = null) where T : class
        {
            try
            {
                var url = endpoint;
                if (parameters != null && parameters.Any())
                {
                    var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                    url = $"{endpoint}?{queryString}";
                }

                // Python'daki requests.get(url, headers=HEADERS)
                var response = await _httpClient.GetAsync(url);

                // Python'daki r.raise_for_status()
                response.EnsureSuccessStatusCode();

                var jsonStream = await response.Content.ReadAsStreamAsync();

                // Python'daki r.json()
                var result = await JsonSerializer.DeserializeAsync<T>(jsonStream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); // Büyük/küçük harf duyarsız

                return result;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"⚠️  API Request Error ({endpoint}): {e.Message}");
            }
            catch (JsonException e)
            {
                Console.WriteLine($"⚠️  JSON Deserialization Error ({endpoint}): {e.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"⚠️  General Error ({endpoint}): {e.Message}");
            }

            return default;
        }
    }
}