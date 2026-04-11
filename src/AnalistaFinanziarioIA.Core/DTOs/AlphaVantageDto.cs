using System.Text.Json.Serialization;

namespace AnalistaFinanziarioIA.Core.DTOs
{
    public class AlphaVantageResponse
    {
        [JsonPropertyName("Global Quote")]
        public GlobalQuote? GlobalQuote { get; set; }
    }

    public class GlobalQuote
    {
        [JsonPropertyName("01. symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("05. price")]
        public string PriceString { get; set; } = "0";

        // Convertiamo la stringa dell'API in decimal
        public decimal Price => decimal.TryParse(PriceString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val) ? val : 0;
    }

    public class AlphaVantageSearchResponse
    {
        [JsonPropertyName("bestMatches")]
        public List<AlphaMatchDto> BestMatches { get; set; } = [];
    }

    public class AlphaMatchDto
    {
        [JsonPropertyName("1. symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("2. name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("3. type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("4. region")]
        public string Region { get; set; } = string.Empty;

        [JsonPropertyName("8. currency")]
        public string Currency { get; set; } = string.Empty;
    }
}