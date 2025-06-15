using System.Drawing;
using System.Globalization;

namespace LifeSim.Utils;

public static class ColorUtils
{
    public static string ToHex(this Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    
    public static Color FromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex) || hex is not ['#', _, _, _, _, _, _])
            throw new ArgumentException("Invalid hex color format. Expected format: #RRGGBB");

        var r = int.Parse(hex.Substring(1, 2), NumberStyles.HexNumber);
        var g = int.Parse(hex.Substring(3, 2), NumberStyles.HexNumber);
        var b = int.Parse(hex.Substring(5, 2), NumberStyles.HexNumber);

        return Color.FromArgb(r, g, b);
    }

    public static Color ColorFromHsla(float h, float s, float l, float a)
    {
        if (0 == s)
        {
            return Color.FromArgb((int)(l * 255), (int)(l * 255), (int)(l * 255), (int)(l * 255));
        }

        var fMax = l < 0.5f ? l * (1 + s) : l + s - l * s;
        var fMin = 2 * l - fMax;
        var sextant = h / 60f;
        var i = (int)Math.Floor(sextant) % 6;
        var fraction = sextant - i;
        var fMid = fraction < 0.5f
            ? fMin + (fMax - fMin) * fraction * 2
            : fMax - (fMax - fMin) * (fraction - 0.5f) * 2;

        var iMax = (int)(fMax * 255);
        var iMid = (int)(fMid * 255);
        var iMin = (int)(fMin * 255);
        var alpha = (int)(a * 255);

        return i switch
        {
            0 => Color.FromArgb(alpha, iMax, iMid, iMin),
            1 => Color.FromArgb(alpha, iMid, iMax, iMin),
            2 => Color.FromArgb(alpha, iMin, iMax, iMid),
            3 => Color.FromArgb(alpha, iMin, iMid, iMax),
            4 => Color.FromArgb(alpha, iMid, iMin, iMax),
            _ => Color.FromArgb(alpha, iMax, iMin, iMid)
        };
    }
}