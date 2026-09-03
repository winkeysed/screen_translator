using System.Windows;
using System.Windows.Input;

namespace ScreenTranslator;

public partial class OverlayWindow : Window
{
    private Point? _start;
    private Rect _current;

    public Rect SelectedRect { get; private set; }
    public bool Cancelled { get; private set; } = true;

    public OverlayWindow()
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.Manual;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Cancelled = true;
            Close();
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Right)
        {
            Cancelled = true;
            Close();
            return;
        }
        if (e.ChangedButton != MouseButton.Left) return;

        _start = e.GetPosition(this);
        Hint.Visibility = Visibility.Collapsed;
        Selection.Visibility = Visibility.Visible;
        SizeBadge.Visibility = Visibility.Visible;
        UpdateSelection(_start.Value);
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_start.HasValue) UpdateSelection(e.GetPosition(this));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || !_start.HasValue) return;

        ReleaseMouseCapture();
        if (_current.Width < 10 || _current.Height < 10)
        {
            Cancelled = true;
        }
        else
        {
            SelectedRect = _current;
            Cancelled = false;
        }
        Close();
    }

    private void UpdateSelection(Point point)
    {
        var start = _start!.Value;
        point.X = Math.Clamp(point.X, 0, ActualWidth);
        point.Y = Math.Clamp(point.Y, 0, ActualHeight);

        var x = Math.Min(start.X, point.X);
        var y = Math.Min(start.Y, point.Y);
        var w = Math.Abs(point.X - start.X);
        var h = Math.Abs(point.Y - start.Y);

        _current = new Rect(x, y, w, h);
        Selection.Margin = new Thickness(x, y, 0, 0);
        Selection.Width = w;
        Selection.Height = h;
        SizeText.Text = $"{Math.Round(w)} × {Math.Round(h)}";

        double badgeTop = y + h + 34 > ActualHeight ? Math.Max(4, y - 34) : y + h + 10;
        SizeBadge.Margin = new Thickness(x, badgeTop, 0, 0);
    }
}
