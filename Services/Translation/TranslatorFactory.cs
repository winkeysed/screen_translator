using ScreenTranslator.Models;

namespace ScreenTranslator.Services.Translation;

public static class TranslatorFactory
{
    public static ITranslator Create(AppSettings settings) => settings.Provider switch
    {
        TranslationProvider.DeepL => new DeepLTranslator(settings.DeeplApiKey, settings.DeeplIsFreeKey),
        TranslationProvider.LibreTranslate => new LibreTranslateTranslator(
            settings.LibreTranslateUrl, settings.LibreTranslateApiKey),
        _ => new GoogleTranslator()
    };
}
