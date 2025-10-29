using System.Text.Json.Serialization;

// DÜZELTME: Doğru Namespace'i kullanıyoruz.
namespace ReverseSignalApp.Services
{
    // Not: C#'ta PascalCase (BüyükHarf) kullanılır.
    // JSON'dan gelen "snake_case" (küçük_harf_alt_tire) veriyi doğru eşleştirmek için
    // [JsonPropertyName] attribute'unu kullanırız.

    #region Python Modellerinin C# Karşılıkları (models.py)

    public record TeamModel(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("short_name")] string? ShortName,
        [property: JsonPropertyName("tla")] string? Tla,
        [property: JsonPropertyName("crest_url")] string? CrestUrl,
        [property: JsonPropertyName("venue")] string? Venue,
        [property: JsonPropertyName("founded")] int? Founded
    );

    public record MatchScore(
        [property: JsonPropertyName("home")] int? Home,
        [property: JsonPropertyName("away")] int? Away
    );

    /// <summary>
    /// Python'daki MatchModel'imizin C# karşılığı.
    /// </summary>
    public record MatchModel
    {
        public int Id { get; set; }
        public DateTime UtcDate { get; set; }
        public string Status { get; set; } = null!; // "NS", "FT", "HT", "25'"
        public string Competition { get; set; } = null!;
        public TeamModel HomeTeam { get; set; } = null!;
        public TeamModel AwayTeam { get; set; } = null!;
        public MatchScore Score { get; set; } = null!;
        public string? Stage { get; set; }
        public string? Group { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    #endregion

    #region API Response Wrapper Sınıflar

    /// <summary>
    /// /fixtures endpoint'inden gelen ana JSON yanıtını temsil eder.
    /// </summary>
    public class ApiFootballResponse
    {
        [JsonPropertyName("response")]
        public List<FixtureWrapper> Response { get; set; } = new();
    }

    /// <summary>
    /// /fixtures/statistics endpoint'inden gelen ana JSON yanıtını temsil eder.
    /// </summary>
    public class ApiStatisticsResponse
    {
        [JsonPropertyName("response")]
        public List<TeamStatisticWrapper> Response { get; set; } = new();
    }

    /// <summary>
    /// /fixtures/events endpoint'inden gelen yanıt.
    /// </summary>
    public class ApiEventsResponse
    {
        [JsonPropertyName("response")]
        public List<EventItem> Response { get; set; } = new();
    }

    #endregion

    #region JSON Deserialization Yardımcıları

    public record FixtureWrapper
    {
        [JsonPropertyName("fixture")]
        public Fixture Fixture { get; set; } = null!;

        [JsonPropertyName("league")]
        public League League { get; set; } = null!;

        [JsonPropertyName("teams")]
        public Teams Teams { get; set; } = null!;

        [JsonPropertyName("goals")]
        public Goals Goals { get; set; } = null!;
    }

    public record Fixture(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("date")] DateTime Date,
        [property: JsonPropertyName("status")] Status Status
    );

    public record Status([property: JsonPropertyName("short")] string Short);

    public record League(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("round")] string Round
    );

    public record Teams(
        [property: JsonPropertyName("home")] TeamModel Home,
        [property: JsonPropertyName("away")] TeamModel Away
    );

    public record Goals(
        [property: JsonPropertyName("home")] int? Home,
        [property: JsonPropertyName("away")] int? Away
    );

    public record TeamStatisticWrapper
    {
        [JsonPropertyName("team")]
        public TeamModel Team { get; set; } = null!;

        [JsonPropertyName("statistics")]
        public List<StatisticItem> Statistics { get; set; } = new();
    }

    public record StatisticItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = null!;

        // Değerler string ("55%") veya int (10) olabileceğinden object kullanıyoruz
        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }

    public record EventItem
    {
        [JsonPropertyName("time")]
        public EventTime Time { get; set; } = null!;
    }

    public record EventTime([property: JsonPropertyName("elapsed")] int? Elapsed);

    #endregion
}