using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace AnalistaFinanziarioIA.API.Plugins;

public class WebSearchPlugin(string _apiKey)
{
    private readonly HttpClient _httpClient = new();

    [KernelFunction, Description("Cerca news finanziarie e informazioni aggiornate sul web.")]
    public async Task<string> SearchAsync([Description("La query di ricerca")] string query)
    {
        var requestBody = new
        {
            api_key = _apiKey,
            query = query,
            search_depth = "basic",
            max_results = 5
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("https://api.tavily.com/search", content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            var results = doc.RootElement.GetProperty("results");

            var sb = new StringBuilder();
            foreach (var result in results.EnumerateArray())
            {
                sb.AppendLine($"- {result.GetProperty("title").GetString()}: {result.GetProperty("content").GetString()} ({result.GetProperty("url").GetString()})");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Errore nella ricerca: {ex.Message}";
        }
    }
}