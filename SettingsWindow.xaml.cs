using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScreenTranslator.Models;
using ScreenTranslator.Services;

namespace ScreenTranslator;

public partial class SettingsWindow : Window
{
    public event Action<AppSettings>? Saved;

    private sealed record OcrOption(string Tag, string Label);

    public SettingsWindow(AppSettings current)
    {
        InitializeComponent();
        LoadIntoUi(current);
    }

    private void LoadIntoUi(AppSettings s)
    {
        HotkeyBox.Text = s.Hotkey;
        ProviderBox.SelectedIndex = (int)s.Provider;
        DeeplKeyBox.Text = s.DeeplApiKey;
        DeeplFreeCheck.IsChecked = s.DeeplIsFreeKey;
        LibreUrlBox.Text = s.LibreTranslateUrl;
        LibreKeyBox.Text = s.LibreTranslateApiKey;
        TargetBox.Text = s.TargetLanguage;
        SourceBox.Text = s.SourceLanguage;
        FontBox.Text = s.ResultFontSize.ToString("0.#", CultureInfo.InvariantCulture);
        AutoHideBox.Text = s.AutoHideSeconds.ToString(CultureInfo.InvariantCulture);
        AutoCopyCheck.IsChecked = s.AutoCopyResult;

        var options = new List<OcrOption> { new("", "Авто (языки системы)") };
        foreach (var tag in OcrService.AvailableLanguages())
        {
            string label;
            try { label = new Windows.Globalization.Language(tag).DisplayName + $"  ({tag})"; }
            catch { label = tag; }
            options.Add(new OcrOption(tag, label));
        }
        OcrCombo.ItemsSource = options;
        OcrCombo.DisplayMemberPath = nameof(OcrOption.Label);
        OcrCombo.SelectedValuePath = nameof(OcrOption.Tag);
        OcrCombo.SelectedValue = string.IsNullOrEmpty(s.OcrLanguage) ? "" : s.OcrLanguage;

        ScaleSlider.Value = s.ImageScale;
    }

    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ScaleLabel != null)
            ScaleLabel.Text = "×" + e.NewValue.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void ProviderBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeeplPanel == null || LibrePanel == null) return;
        DeeplPanel.Visibility = ProviderBox.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        LibrePanel.Visibility = ProviderBox.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin or Key.Escape or Key.Tab)
            return;

        var sb = new System.Text.StringBuilder();
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) sb.Append("Ctrl+");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) sb.Append("Shift+");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) sb.Append("Alt+");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) sb.Append("Win+");

        var token = HotkeyService.TokenFromKey(key);
        if (token.Length == 0 || sb.Length == 0)
        {
            MessageBox.Show(this,
                "Нужна буква/цифра/F-клавиша и минимум один модификатор (Ctrl/Shift/Alt/Win).",
                "ScreenTranslator", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        HotkeyBox.Text = sb.Append(token).ToString();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var hotkey = HotkeyBox.Text.Trim();
        if (!HotkeyService.TryParse(hotkey, out _, out _))
        {
            Warn("Некорректное сочетание клавиш.");
            return;
        }

        var provider = (TranslationProvider)ProviderBox.SelectedIndex;

        var deeplKey = DeeplKeyBox.Text.Trim();
        if (provider == TranslationProvider.DeepL && deeplKey.Length == 0)
        {
            Warn("Для DeepL нужно указать API-ключ.");
            return;
        }

        var libreUrl = LibreUrlBox.Text.Trim();
        if (provider == TranslationProvider.LibreTranslate &&
            (!Uri.TryCreate(libreUrl, UriKind.Absolute, out var uri) ||
             uri.Scheme is not ("http" or "https")))
        {
            Warn("Некорректный адрес LibreTranslate (пример: https://libretranslate.com/translate).");
            return;
        }

        var target = TargetBox.Text.Trim().ToLowerInvariant();
        if (target.Length == 0)
        {
            Warn("Укажите язык перевода (например ru).");
            return;
        }

        if (!TryParseDouble(FontBox.Text, out var font) || font is < 8 or > 40)
        {
            Warn("Размер шрифта: число от 8 до 40.");
            return;
        }

        if (!TryParseInt(AutoHideBox.Text, out var hide) || hide is < 0 or > 600)
        {
            Warn("Автоскрытие: целое число секунд от 0 до 600 (0 — не скрывать).");
            return;
        }

        var saved = new AppSettings
        {
            Hotkey = hotkey,
            Provider = provider,
            DeeplApiKey = deeplKey,
            DeeplIsFreeKey = DeeplFreeCheck.IsChecked == true,
            LibreTranslateUrl = libreUrl,
            LibreTranslateApiKey = LibreKeyBox.Text.Trim(),
            SourceLanguage = string.IsNullOrWhiteSpace(SourceBox.Text) ? "auto" : SourceBox.Text.Trim(),
            TargetLanguage = target,
            OcrLanguage = OcrCombo.SelectedValue as string ?? "",
            ImageScale = ScaleSlider.Value,
            AutoCopyResult = AutoCopyCheck.IsChecked == true,
            AutoHideSeconds = hide,
            ResultFontSize = font
        };

        try
        {
            SettingsService.Save(saved);
        }
        catch (Exception ex)
        {
            Warn("Не удалось сохранить настройки: " + ex.Message);
            return;
        }

        Saved?.Invoke(saved);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void Warn(string message) =>
        MessageBox.Show(this, message, "ScreenTranslator", MessageBoxButton.OK, MessageBoxImage.Warning);

    private static bool TryParseDouble(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        return double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float,
            CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseInt(string? text, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        return int.TryParse(text.Trim(), out value);
    }
}
