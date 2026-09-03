using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace ScreenTranslator.Services;

public sealed class HotkeyService : IDisposable
{
    public const int WmHotkey = 0x0312;

    private const int Id = 1;
    private const uint ModAlt = 0x1;
    private const uint ModControl = 0x2;
    private const uint ModShift = 0x4;
    private const uint ModWin = 0x8;
    private const uint ModNoRepeat = 0x4000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private IntPtr _hwnd;
    private HwndSource? _source;

    public event Action? Pressed;

    public void Attach(HwndSource source)
    {
        _source = source;
        _hwnd = source.Handle;
        source.AddHook(WndProc);
    }

    public bool Register(string combination)
    {
        Unregister();
        if (_hwnd == IntPtr.Zero) return false;
        if (!TryParse(combination, out var modifiers, out var key)) return false;
        return RegisterHotKey(_hwnd, Id, modifiers | ModNoRepeat, key);
    }

    public void Unregister()
    {
        if (_hwnd != IntPtr.Zero) UnregisterHotKey(_hwnd, Id);
    }

    public void Dispose() => Unregister();

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == Id)
        {
            Pressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public static string TokenFromKey(Key key)
    {
        if (key is >= Key.A and <= Key.Z) return key.ToString();
        if (key is >= Key.D0 and <= Key.D9) return ((char)('0' + (int)(key - Key.D0))).ToString();
        if (key is >= Key.NumPad0 and <= Key.NumPad9) return "Num" + (int)(key - Key.NumPad0);
        if (key is >= Key.F1 and <= Key.F24) return key.ToString();
        return "";
    }

    public static bool TryParse(string? combination, out uint modifiers, out uint key)
    {
        modifiers = 0;
        key = 0;
        if (string.IsNullOrWhiteSpace(combination)) return false;

        var parts = combination.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts.Take(parts.Length - 1))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control": modifiers |= ModControl; break;
                case "shift": modifiers |= ModShift; break;
                case "alt": modifiers |= ModAlt; break;
                case "win":
                case "windows": modifiers |= ModWin; break;
                default: return false;
            }
        }
        if (modifiers == 0) return false;

        var last = parts[^1];
        var up = last.ToUpperInvariant();
        if (up.Length == 1 && char.IsLetterOrDigit(up[0]))
            key = up[0];
        else if (up.Length == 4 && up.StartsWith("NUM", StringComparison.Ordinal) && char.IsDigit(up[3]))
            key = (uint)(0x60 + (up[3] - '0'));
        else if (up.Length is > 1 and <= 3 && up[0] == 'F' && int.TryParse(up[1..], out var f) && f is >= 1 and <= 24)
            key = (uint)(0x70 + f - 1);
        else
            return false;

        return key != 0;
    }
}
