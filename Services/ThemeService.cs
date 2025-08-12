using SkiaSharp;

namespace SSHExplorer.Services;

public sealed class ThemeService : IThemeService
{
    public void ApplyLightTheme(Color primary)
    {
        Application.Current!.Resources["PrimaryColor"] = primary;
        Application.Current!.UserAppTheme = AppTheme.Light;
    }

    public void ApplyDarkTheme(Color primary)
    {
        Application.Current!.Resources["PrimaryColor"] = primary;
        Application.Current!.UserAppTheme = AppTheme.Dark;
    }

    public Color ComputePrimaryFromFolder(string folder)
    {
        try
        {
            if (!Directory.Exists(folder)) return Colors.CornflowerBlue;
            var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".bmp" };
            var file = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                .FirstOrDefault(p => exts.Contains(Path.GetExtension(p)));
            if (file is null) return Colors.CornflowerBlue;
            using var stream = File.OpenRead(file);
            using var bmp = SKBitmap.Decode(stream);
            if (bmp is null) return Colors.CornflowerBlue;
            long r=0,g=0,b=0; long count=0;
            for (int y = 0; y < bmp.Height; y+= Math.Max(1, bmp.Height/300))
            {
                for (int x = 0; x < bmp.Width; x+= Math.Max(1, bmp.Width/300))
                {
                    var c = bmp.GetPixel(x, y);
                    r += c.Red; g += c.Green; b += c.Blue; count++;
                }
            }
            if (count == 0) return Colors.CornflowerBlue;
            var cr = (byte)(r / count); var cg = (byte)(g / count); var cb = (byte)(b / count);
            return Color.FromRgb(cr, cg, cb);
        }
        catch { return Colors.CornflowerBlue; }
    }
}
