using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows;

public sealed partial class AboutWindow
{
    private const string MadeByText = $"Made by {Author}";

    private static void DrawFooter()
    {
        var s = ImGuiHelpers.GlobalScale;
        Styling.HairlineRule();
        Styling.VSpace(5);

        var glyph = Glyph.Of(FontAwesomeIcon.Code);
        var twinkle = Styling.Pulse(2600.0);
        Vector2 glyphSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            glyphSize = ImGui.CalcTextSize(glyph);
        var gap = 6f * s;
        var total = glyphSize.X + gap + ImGui.CalcTextSize(MadeByText).X;
        Styling.CenterNextItem(total);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, Vector4.Lerp(Styling.AccentBlue, Lighten(Styling.AccentBlueSoft, 0.3f), twinkle)))
            ImGui.TextUnformatted(glyph);
        ImGui.SameLine(0, gap);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(MadeByText);
    }
}
