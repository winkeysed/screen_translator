using System.Net.Http;
using System.Text.Json;

namespace ScreenTranslator.Services.Translation;

public sealed class DeepLTranslator : ITranslator
{
    private readonly string _apiKey;
    private readonly bool _isFree;

    public DeepLTranslator(string apiKey, bool isFree)
    {
        _apiKey = apiKey.Trim();
        _isFree = isFree;
    }

    public string Name => "DeepL";

    public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        if (_apiKey.Length == 0)
            throw new InvalidOperationException("Не указан API-ключ DeepL.");

        var parameters = new List<KeyValuePair<string, string>>
        {
            new("auth_key", _apiKey),
            new("text", text),
            new("target_lang", Normalize(targetLanguage))
        };
        var source = NormalizeOptional(sourceLanguage);
        if (source != null) parameters.Add(new("source_lang", source));

        var host = _isFree ? "https://api-free.deepl.com" : "https://api.deepl.com";
        using var response = await TransHttp.Client.PostAsync(
            host + "/v2/translate", new FormUrlEncodedContent(parameters));
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("translations")[0].GetProperty("text").GetString() ?? "";
    }

    private static string Normalize(string code) => code.Split('-')[0].ToUpperInvariant();

    private static string? NormalizeOptional(string? code)
    {
        var trimmed = code?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return null;
        return Normalize(trimmed);
    }
}
