using System.Drawing;

namespace LifeSim;

public static class ColorUtils
{
    public static string ToHex(this Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}