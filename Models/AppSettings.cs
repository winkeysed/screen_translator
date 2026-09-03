namespace ScreenTranslator.Models;

public enum TranslationProvider
{
    Google,
    DeepL,
    LibreTranslate
}

public sealed class AppSettings
{
    public string Hotkey { get; set; } = "Win+Shift+D";

    public TranslationProvider Provider { get; set; } = TranslationProvider.Google;

    public string DeeplApiKey { get; set; } = "";
    public bool DeeplIsFreeKey { get; set; } = true;

    public string LibreTranslateUrl { get; set; } = "https://libretranslate.com/translate";
    public string LibreTranslateApiKey { get; set; } = "";

    public string SourceLanguage { get; set; } = "auto";
    public string TargetLanguage { get; set; } = "ru";

    public string OcrLanguage { get; set; } = "";

    public double ImageScale { get; set; } = 2.0;

    public bool AutoCopyResult { get; set; }
    public int AutoHideSeconds { get; set; }
    public double ResultFontSize { get; set; } = 14.0;

    public AppSettings Clone() => (AppSettings)MemberwiseClone();
}
