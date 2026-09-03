namespace ScreenTranslator.Services.Translation;

public interface ITranslator
{
    string Name { get; }
    Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
}
