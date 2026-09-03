using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScreenTranslator.Models;

namespace ScreenTranslator.Services;

public static class SettingsService
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenTranslator");

    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options) ?? new AppSettings();
        }
        catch
        {
        }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options));
    }

    public static string FilePathForDiagnostics => FilePath;
}
