namespace AutoPvpSeriesGrind.Windows.Components;

internal static class ListReorder
{
    public static bool Apply<T>(IList<T> list, int? moveUp, int? moveDown, int? remove)
    {
        if (moveUp is { } upIndex && upIndex > 0)
        {
            (list[upIndex - 1], list[upIndex]) = (list[upIndex], list[upIndex - 1]);
            return true;
        }

        if (moveDown is { } downIndex && downIndex < list.Count - 1)
        {
            (list[downIndex + 1], list[downIndex]) = (list[downIndex], list[downIndex + 1]);
            return true;
        }

        if (remove is { } removeIndex)
        {
            list.RemoveAt(removeIndex);
            return true;
        }

        return false;
    }
}
