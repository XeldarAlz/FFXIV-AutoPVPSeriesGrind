using Dalamud.Interface;

namespace AutoPvpSeriesGrind.Windows;

internal static class Glyph
{
    private static readonly Dictionary<FontAwesomeIcon, string> GlyphCache = new();
    private static readonly Dictionary<(FontAwesomeIcon Icon, string Id), string> LabelCache = new();

    public static string Of(FontAwesomeIcon icon)
    {
        if (GlyphCache.TryGetValue(icon, out var cached))
        {
            return cached;
        }
        var glyph = icon.ToIconString();
        GlyphCache[icon] = glyph;
        return glyph;
    }

    public static string Labeled(FontAwesomeIcon icon, string id)
    {
        var key = (icon, id);
        if (LabelCache.TryGetValue(key, out var cached))
        {
            return cached;
        }
        var label = Of(icon) + id;
        LabelCache[key] = label;
        return label;
    }
}
