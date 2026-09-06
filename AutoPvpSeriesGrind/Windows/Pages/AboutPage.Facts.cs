using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Pages;

internal sealed partial class AboutPage
{
    private readonly record struct FactCategory(FontAwesomeIcon Icon, LocString Header, Vector4 Color, LocString[] Lines);

    private static readonly FactCategory[] Categories =
    {
        new(FontAwesomeIcon.Heart, L.About.ReminderTitle, Styling.AccentRose, L.About.Reminders),
        new(FontAwesomeIcon.Lightbulb, L.About.FactsTitle, Styling.AccentAmberSoft, L.About.Facts),
        new(FontAwesomeIcon.Star, L.About.QuotesTitle, Styling.AccentMintSoft, L.About.Quotes),
        new(FontAwesomeIcon.GrinBeam, L.About.JokesTitle, Styling.AccentBlueSoft, L.About.Jokes),
    };

    private static readonly int[][] factBags = new int[Categories.Length][];
    private static readonly int[] factBagPositions = new int[Categories.Length];
    private static readonly int[] factLastServed = new int[Categories.Length];

    private static int factCategoryIndex = -1;
    private static int factLineIndex;
    private static bool iconHovered;

    private static void IconEasterEgg(Vector2 min, Vector2 max)
    {
        if (!Hit.HoveringRect(min, max))
        {
            iconHovered = false;
            return;
        }

        if (!iconHovered)
        {
            iconHovered = true;
            factCategoryIndex = (factCategoryIndex + 1) % Categories.Length;
            factLineIndex = NextLineInCategory(factCategoryIndex);
        }

        var category = Categories[Math.Max(0, factCategoryIndex)];
        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        using (Tooltip.Begin())
        {
            var scale = ImGuiHelpers.GlobalScale;
            var origin = ImGui.GetCursorScreenPos();
            var iconSize = TextDraw.IconSize(category.Icon);
            var header = Loc.T(category.Header);
            var headerSize = TextDraw.Measure(header);
            TextDraw.Icon(category.Icon, new Vector2(origin.X, origin.Y + (headerSize.Y - iconSize.Y) * 0.5f), category.Color);
            TextDraw.At(header, new Vector2(origin.X + iconSize.X + 6f * scale, origin.Y), category.Color);
            ImGui.Dummy(new Vector2(iconSize.X + 6f * scale + headerSize.X, headerSize.Y));
            Styling.VSpace(4f);
            Tooltip.Text(Loc.T(category.Lines[factLineIndex]));
        }
    }

    private static int NextLineInCategory(int categoryIndex)
    {
        var count = Categories[categoryIndex].Lines.Length;
        if (factBags[categoryIndex] is null || factBagPositions[categoryIndex] >= count)
        {
            var avoidFirst = factBags[categoryIndex] is null ? -1 : factLastServed[categoryIndex];
            factBags[categoryIndex] = Shuffle(count, avoidFirst);
            factBagPositions[categoryIndex] = 0;
        }

        var line = factBags[categoryIndex]![factBagPositions[categoryIndex]++];
        factLastServed[categoryIndex] = line;
        return line;
    }

    private static int[] Shuffle(int count, int avoidFirst)
    {
        var order = new int[count];
        for (var index = 0; index < count; index++) order[index] = index;
        for (var index = count - 1; index > 0; index--)
        {
            var swap = Random.Shared.Next(index + 1);
            (order[index], order[swap]) = (order[swap], order[index]);
        }

        if (count > 1 && order[0] == avoidFirst)
        {
            var swap = 1 + Random.Shared.Next(count - 1);
            (order[0], order[swap]) = (order[swap], order[0]);
        }

        return order;
    }
}
