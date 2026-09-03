using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ScreenTranslator.Services;

public static class CaptureService
{
    public static Bitmap Capture(Rectangle physicalRect)
    {
        var bitmap = new Bitmap(
            Math.Max(1, physicalRect.Width),
            Math.Max(1, physicalRect.Height),
            PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(physicalRect.X, physicalRect.Y, 0, 0, physicalRect.Size);
        }
        return bitmap;
    }

    public static Bitmap Upscale(Bitmap source, double factor)
    {
        if (factor <= 1.01) return source;
        var w = Math.Min((int)(source.Width * factor), 5000);
        var h = Math.Min((int)(source.Height * factor), 5000);
        if (w <= source.Width && h <= source.Height) return source;

        var scaled = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(scaled))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, new Rectangle(0, 0, w, h));
        }
        source.Dispose();
        return scaled;
    }
}
