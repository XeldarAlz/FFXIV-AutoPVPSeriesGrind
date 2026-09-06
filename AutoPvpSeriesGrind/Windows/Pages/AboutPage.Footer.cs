using AutoPvpSeriesGrind.Core.Localization;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Pages;

internal sealed partial class AboutPage
{
    private static void DrawFooter()
    {
        var madeByText = Loc.T(L.About.MadeBy, Author);
        var scale = ImGuiHelpers.GlobalScale;
        Paint.Divider(6f);

        using var font = Fonts.PushCaption();
        var twinkle = Styling.Pulse(2600.0);
        var glyphSize = TextDraw.IconSize(FontAwesomeIcon.Code);
        var textSize = TextDraw.Measure(madeByText);
        var gap = 6f * scale;
        var total = glyphSize.X + gap + textSize.X;

        var origin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        var startX = origin.X + MathF.Max(0f, (avail - total) * 0.5f);
        var midY = origin.Y + MathF.Max(glyphSize.Y, textSize.Y) * 0.5f;

        TextDraw.Icon(FontAwesomeIcon.Code, new Vector2(startX, midY - glyphSize.Y * 0.5f),
            Vector4.Lerp(Styling.AccentBlue, Styling.Lighten(Styling.AccentBlueSoft, 0.3f), twinkle));
        TextDraw.At(madeByText, new Vector2(startX + glyphSize.X + gap, midY - textSize.Y * 0.5f), Styling.TextDim);

        ImGui.Dummy(new Vector2(avail, MathF.Max(glyphSize.Y, textSize.Y)));
    }
}
