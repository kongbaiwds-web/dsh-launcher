using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace DSHLauncher;

/// <summary>运行时绘制应用图标（深蓝圆角方块 + 白色 D 字），供窗口和托盘使用。</summary>
internal static class AppIcon
{
    public static Icon Create()
    {
        using var bitmap = new Bitmap(64, 64);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new Rectangle(2, 2, 60, 60), 14);
            using var bg = new SolidBrush(Color.FromArgb(23, 42, 84));
            g.FillPath(bg, path);
            using var border = new Pen(Color.FromArgb(110, 140, 200), 2f);
            g.DrawPath(border, path);
            using var font = new Font("Segoe UI", 32f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var white = new SolidBrush(Color.White);
            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            g.DrawString("D", font, white, new RectangleF(0, 0, 64, 64), format);
        }

        IntPtr hIcon = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(hIcon);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
