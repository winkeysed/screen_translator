using System.Text;
using System.Text.Json;

namespace ScreenTranslator.Services.Translation;

public sealed class GoogleTranslator : ITranslator
{
    public string Name => "Google";

    public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        var sl = Source(sourceLanguage);
        var url = "https://translate.googleapis.com/translate_a/single" +
                  "?client=gtx&dt=t" +
                  "&sl=" + Uri.EscapeDataString(sl) +
                  "&tl=" + Uri.EscapeDataString(targetLanguage.Trim()) +
                  "&q=" + Uri.EscapeDataString(text);

        using var response = await TransHttp.Client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var sb = new StringBuilder();
        foreach (var segment in doc.RootElement[0].EnumerateArray())
            sb.Append(segment[0].GetString());
        return sb.ToString();
    }

    private static string Source(string? sourceLanguage)
    {
        var trimmed = sourceLanguage?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return "auto";
        return trimmed.ToLowerInvariant();
    }
}
