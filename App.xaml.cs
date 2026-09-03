using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using ScreenTranslator.Models;
using ScreenTranslator.Services;
using ScreenTranslator.Services.Translation;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace ScreenTranslator;

public partial class App : Application
{
    private Mutex? _mutex;
    private WinForms.NotifyIcon? _tray;
    private HotkeyService? _hotkeys;
    private AppSettings _settings = new();
    private SettingsWindow? _settingsWindow;
    private bool _busy;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "ScreenTranslator.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            Shutdown();
            return;
        }
        base.OnStartup(e);
        _settings = SettingsService.Load();

        var mainWindow = new Window
        {
            Width = 0,
            Height = 0,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
            Visibility = Visibility.Hidden
        };
        MainWindow = mainWindow;
        mainWindow.Show();

        var source = (HwndSource)PresentationSource.FromVisual(mainWindow)!;
        _hotkeys = new HotkeyService();
        _hotkeys.Attach(source);
        _hotkeys.Pressed += RunTranslation;
        if (!_hotkeys.Register(_settings.Hotkey))
            Balloon("ScreenTranslator",
                $"Сочетание \"{_settings.Hotkey}\" занято другой программой — поменяйте его в настройках.");

        InitTray();
    }

    private void InitTray()
    {
        _tray = new WinForms.NotifyIcon
        {
            Icon = MakeIcon(),
            Text = "ScreenTranslator",
            Visible = true
        };
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Перевести область", null, (_, _) => RunTranslation());
        menu.Items.Add("Настройки", null, (_, _) => OpenSettings());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => ExitApp());
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => RunTranslation();
    }

    private async void RunTranslation()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            if (!OcrService.IsReady)
            {
                ResultWindow.ShowInfo("OCR недоступен",
                    "Не найдено ни одного языка распознавания.\n\n" +
                    "Установите языковой пакет Windows:\n" +
                    "Параметры → Время и язык → Язык и регион → Добавить язык.\n" +
                    "Затем перезапустите приложение.",
                    InfoAnchor());
                return;
            }

            var overlay = new OverlayWindow { Owner = MainWindow };
            overlay.ShowDialog();
            if (overlay.Cancelled) return;

            var dpi = VisualTreeHelper.GetDpi(overlay);
            var sys = WinForms.SystemInformation.VirtualScreen;
            var sel = overlay.SelectedRect;
            var physical = new Drawing.Rectangle(
                sys.Left + (int)Math.Round(sel.X * dpi.DpiScaleX),
                sys.Top + (int)Math.Round(sel.Y * dpi.DpiScaleY),
                Math.Max(1, (int)Math.Round(sel.Width * dpi.DpiScaleX)),
                Math.Max(1, (int)Math.Round(sel.Height * dpi.DpiScaleY)));

            using var shot = CaptureService.Upscale(CaptureService.Capture(physical), _settings.ImageScale);
            var recognized = await OcrService.RecognizeAsync(shot, _settings);
            var anchor = AnchorFor(sel);

            if (string.IsNullOrWhiteSpace(recognized))
            {
                ResultWindow.ShowInfo("Текст не найден",
                    "В выделенной области не удалось распознать текст.\n\n" +
                    "Попробуйте выделить область точнее или увеличьте масштаб снимка в настройках.",
                    anchor);
                return;
            }

            var translator = TranslatorFactory.Create(_settings);
            string translated;
            try
            {
                translated = await translator.TranslateAsync(
                    recognized, _settings.SourceLanguage, _settings.TargetLanguage);
            }
            catch (Exception ex)
            {
                ResultWindow.ShowInfo($"{translator.Name}: ошибка перевода",
                    ex.Message + HintFor(_settings.Provider), anchor);
                return;
            }

            if (_settings.AutoCopyResult) TrySetClipboard(translated);

            ResultWindow.ShowTranslation(anchor,
                $"{translator.Name}  →  {_settings.TargetLanguage}",
                translated, recognized, _settings);
        }
        catch (Exception ex)
        {
            ResultWindow.ShowInfo("Ошибка", ex.Message, InfoAnchor());
        }
        finally
        {
            _busy = false;
        }
    }

    private static Rect InfoAnchor()
    {
        var wa = SystemParameters.WorkArea;
        return new Rect(wa.Left + wa.Width / 2 - 215, wa.Top + 90, 430, 10);
    }

    private static Rect AnchorFor(Rect sel) => new(
        SystemParameters.VirtualScreenLeft + sel.X,
        SystemParameters.VirtualScreenTop + sel.Y,
        sel.Width,
        sel.Height);

    private static string HintFor(TranslationProvider provider) => provider switch
    {
        TranslationProvider.DeepL => "\n\nПроверьте API-ключ и тип аккаунта (бесплатный/платный) в настройках.",
        TranslationProvider.LibreTranslate => "\n\nПроверьте адрес сервера и API-ключ в настройках.",
        _ => "\n\nЭтот эндпоинт Google неофициальный и может быть недоступен из вашей сети."
    };

    private static void TrySetClipboard(string text)
    {
        try { Clipboard.SetText(text); } catch { }
    }

    private void OpenSettings()
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(_settings);
        _settingsWindow.Saved += saved =>
        {
            _settings = saved;
            if (_hotkeys != null && !_hotkeys.Register(saved.Hotkey))
                Balloon("ScreenTranslator",
                    $"Сочетание \"{saved.Hotkey}\" занято другой программой — выберите другое.");
        };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void Balloon(string title, string message) =>
        _tray?.ShowBalloonTip(4000, title, message, WinForms.ToolTipIcon.Info);

    private static Drawing.Icon MakeIcon()
    {
        using var bmp = new Drawing.Bitmap(32, 32);
        using (var g = Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias;
            using var circle = new Drawing.SolidBrush(Drawing.Color.FromArgb(58, 160, 255));
            g.FillEllipse(circle, 0, 0, 31, 31);
            using var font = new Drawing.Font("Segoe UI", 18f, Drawing.FontStyle.Bold, Drawing.GraphicsUnit.Pixel);
            var size = g.MeasureString("T", font);
            g.DrawString("T", font, Drawing.Brushes.White, (32 - size.Width) / 2f, (32 - size.Height) / 2f);
        }
        return Drawing.Icon.FromHandle(bmp.GetHicon());
    }

    private void ExitApp()
    {
        _hotkeys?.Unregister();
        if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _tray = null;
        }
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ExitApp();
        base.OnExit(e);
    }
}
