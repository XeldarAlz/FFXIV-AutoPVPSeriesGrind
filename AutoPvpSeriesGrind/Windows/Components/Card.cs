using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class Card
{
    public static CardScope Begin(string id, Vector2 size, Vector4 background, Vector4 border, float borderSize = 1f)
    {
        var style = Styling.PushCardStyle();
        var backgroundColor = ImRaii.PushColor(ImGuiCol.ChildBg, background);
        var borderColor = ImRaii.PushColor(ImGuiCol.Border, border);
        var borderSizeStyle = ImRaii.PushStyle(ImGuiStyleVar.ChildBorderSize, borderSize);
        var child = ImRaii.Child(id, size, true,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        return new CardScope(child, borderSizeStyle, borderColor, backgroundColor, style);
    }

    public ref struct CardScope
    {
        private ImRaii.ChildDisposable child;
        private readonly IDisposable borderSizeStyle;
        private readonly IDisposable borderColor;
        private readonly IDisposable backgroundColor;
        private readonly IDisposable style;

        internal CardScope(ImRaii.ChildDisposable child, IDisposable borderSizeStyle, IDisposable borderColor, IDisposable backgroundColor, IDisposable style)
        {
            this.child = child;
            this.borderSizeStyle = borderSizeStyle;
            this.borderColor = borderColor;
            this.backgroundColor = backgroundColor;
            this.style = style;
        }

        public void Dispose()
        {
            child.Dispose();
            borderSizeStyle.Dispose();
            borderColor.Dispose();
            backgroundColor.Dispose();
            style.Dispose();
        }
    }
}
