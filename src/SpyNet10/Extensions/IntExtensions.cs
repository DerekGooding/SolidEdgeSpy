namespace SpyNet10.Extensions;

public static class IntExtensions
{
    public static Color ToColor(this int i)
    {
        var rgb = BitConverter.GetBytes(i);
        return Color.FromArgb(255, rgb[0], rgb[1], rgb[2]);
    }
}