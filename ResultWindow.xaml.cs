using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ScreenTranslator.Models;

namespace ScreenTranslator;

public partial class ResultWindow : Window
{
    private string _translated = "";
    private string _original = "";
    private DispatcherTimer? _hideTimer;

    public ResultWindow()
    {
        InitializeComponent();
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
    }

    public static void ShowTranslation(Rect anchor, string header, string translated, string original, AppSettings settings)
    {
        var w = new ResultWindow();
        w._translated = translated;
        w._original = original;
        w.HeaderText.Text = header;
        w.BodyBox.Text = translated;
        w.BodyBox.FontSize = settings.ResultFontSize;
        w.FooterText.Text = original.Length > 140 ? original[..140] + "…" : original;
        w.FooterText.ToolTip = original;
        w.Root.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 160, 255));
        w.PositionAndShow(anchor, settings);
    }

    public static void ShowInfo(string header, string message, Rect anchor)
    {
        var w = new ResultWindow();
        w.HeaderText.Text = header;
        w.BodyBox.Text = message;
        w.OriginalCheck.Visibility = Visibility.Collapsed;
        w.CopyButton.Visibility = Visibility.Collapsed;
        w.FooterText.Text = "";
        w.Root.BorderBrush = new SolidColorBrush(Color.FromRgb(205, 92, 92));
        w.PositionAndShow(anchor, null);
    }

    private void PositionAndShow(Rect anchor, AppSettings? settings)
    {
        if (settings is { AutoHideSeconds: > 0 })
        {
            _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(settings.AutoHideSeconds) };
            _hideTimer.Tick += (_, _) => Close();
            _hideTimer.Start();
            Root.MouseEnter += (_, _) => _hideTimer?.Stop();
        }

        Opacity = 0;
        Left = -20000;
        Top = -20000;
        Show();

        var vx = SystemParameters.VirtualScreenLeft;
        var vy = SystemParameters.VirtualScreenTop;
        var vw = SystemParameters.VirtualScreenWidth;
        var vh = SystemParameters.VirtualScreenHeight;

        double left = anchor.Right + 12;
        if (left + ActualWidth > vx + vw)
            left = anchor.Left - ActualWidth - 12;
        left = Math.Clamp(left, vx, Math.Max(vx, vx + vw - ActualWidth));

        double top = Math.Clamp(anchor.Top, vy, Math.Max(vy, vy + vh - ActualHeight));

        Left = left;
        Top = top;
        BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(140)));
    }

    private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;
        try { DragMove(); } catch { }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(BodyBox.Text); } catch { }
        CopyButton.Content = "Скопировано";
        CopyButton.IsEnabled = false;
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
        t.Tick += (_, _) =>
        {
            CopyButton.Content = "Копировать";
            CopyButton.IsEnabled = true;
            t.Stop();
        };
        t.Start();
    }

    private void OriginalCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        BodyBox.Text = OriginalCheck.IsChecked == true ? _original : _translated;
    }
}
