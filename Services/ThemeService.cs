using SkiaSharp;

namespace SSHExplorer.Services;

public sealed class ThemeService : IThemeService
{
    public void ApplyLightTheme(Color primary)
    {
    var (primaryDark, secondary, secondaryDarkText) = ComputeVariants(primary, isDark:false);
    var res = Application.Current!.Resources;
    res["Primary"] = primary;
    res["PrimaryDark"] = primaryDark;
    res["Secondary"] = secondary;
    res["SecondaryDarkText"] = secondaryDarkText;
    Application.Current!.UserAppTheme = AppTheme.Light;
    }

    public void ApplyDarkTheme(Color primary)
    {
    var (primaryDark, secondary, secondaryDarkText) = ComputeVariants(primary, isDark:true);
    var res = Application.Current!.Resources;
    res["Primary"] = primary;
    res["PrimaryDark"] = primaryDark;
    res["Secondary"] = secondary;
    res["SecondaryDarkText"] = secondaryDarkText;
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

    private static (Color PrimaryDark, Color Secondary, Color SecondaryDarkText) ComputeVariants(Color baseColor, bool isDark)
    {
        // Convert to SKColor for manipulation
        var sk = new SKColor((byte)(baseColor.Red * 255), (byte)(baseColor.Green * 255), (byte)(baseColor.Blue * 255));
        // Slightly adjust for contrast
        var primaryDarkSk = AdjustLuminance(sk, isDark ? -0.15f : 0.15f);
        var secondarySk = AdjustLuminance(sk, isDark ? -0.4f : 0.4f);
        var secondaryTextSk = AdjustLuminance(sk, isDark ? 0.35f : -0.35f);
        return (ToMaui(primaryDarkSk), ToMaui(secondarySk), ToMaui(secondaryTextSk));
    }

    private static SKColor AdjustLuminance(SKColor color, float delta)
    {
        // Convert to HSV for better control
        color.ToHsv(out float h, out float s, out float v);
        v = Math.Clamp(v + delta, 0f, 1f);
        return SKColor.FromHsv(h, s, v);
    }

    private static Color ToMaui(SKColor c) => Color.FromRgb(c.Red, c.Green, c.Blue);
}
