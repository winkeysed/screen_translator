using System.Net.Http;

namespace ScreenTranslator.Services.Translation;

internal static class TransHttp
{
    public static readonly HttpClient Client = Create();

    private static HttpClient Create()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ScreenTranslator/1.0");
        return client;
    }
}
