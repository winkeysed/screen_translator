using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ScreenTranslator.Services.Translation;

public sealed class LibreTranslateTranslator : ITranslator
{
    private readonly string _url;
    private readonly string _apiKey;

    public LibreTranslateTranslator(string url, string apiKey)
    {
        _url = url.Trim();
        _apiKey = apiKey?.Trim() ?? "";
    }

    public string Name => "LibreTranslate";

    public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        if (!Uri.TryCreate(_url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("Некорректный адрес сервера LibreTranslate.");

        var payload = new
        {
            q = text,
            source = Source(sourceLanguage),
            target = targetLanguage.Trim(),
            format = "text",
            api_key = _apiKey
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await TransHttp.Client.PostAsync(uri, content);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("translatedText").GetString() ?? "";
    }

    private static string Source(string? sourceLanguage)
    {
        var trimmed = sourceLanguage?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return "auto";
        return trimmed.ToLowerInvariant();
    }
}
