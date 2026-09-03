using System.Drawing.Imaging;
using System.IO;
using Windows.Graphics.Imaging;
using Windows.Globalization;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using ScreenTranslator.Models;

namespace ScreenTranslator.Services;

public static class OcrService
{
    public static bool IsReady
    {
        get
        {
            try { return OcrEngine.AvailableRecognizerLanguages.Count > 0; }
            catch { return false; }
        }
    }

    public static IReadOnlyList<string> AvailableLanguages()
    {
        try
        {
            return OcrEngine.AvailableRecognizerLanguages.Select(l => l.LanguageTag).ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public static async Task<string> RecognizeAsync(System.Drawing.Bitmap bitmap, AppSettings settings)
    {
        if (!OcrEngine.AvailableRecognizerLanguages.Any())
            throw new InvalidOperationException(
                "Не найдено ни одного языка распознавания. Установите языковой пакет Windows и перезапустите приложение.");

        OcrEngine? engine = null;
        var tag = settings.OcrLanguage;
        if (!string.IsNullOrWhiteSpace(tag))
        {
            try { engine = OcrEngine.TryCreateFromLanguage(new Language(tag)); }
            catch { }
        }
        engine ??= OcrEngine.TryCreateFromUserProfileLanguages();
        engine ??= OcrEngine.TryCreateFromLanguage(OcrEngine.AvailableRecognizerLanguages.First());

        if (engine == null)
            throw new InvalidOperationException("Не удалось инициализировать OCR-движок Windows.");

        using var softwareBitmap = await ToSoftwareBitmapAsync(bitmap);
        var result = await engine.RecognizeAsync(softwareBitmap);
        return string.Join(Environment.NewLine, result.Lines.Select(l => l.Text));
    }

    private static async Task<SoftwareBitmap> ToSoftwareBitmapAsync(System.Drawing.Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);

        var stream = new InMemoryRandomAccessStream();
        var writer = new DataWriter(stream);
        writer.WriteBytes(ms.ToArray());
        await writer.StoreAsync();
        await writer.FlushAsync();
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        return await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    }
}
