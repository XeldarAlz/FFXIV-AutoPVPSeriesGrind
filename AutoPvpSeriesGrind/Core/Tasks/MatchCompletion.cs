namespace AutoPvpSeriesGrind.Core.Tasks;

// Completion evidence belongs to one duty. Leaving the territory alone is not evidence.
internal sealed class MatchCompletion
{
    public uint Territory { get; private set; }
    public bool Active => Territory != 0;
    public bool Confirmed { get; private set; }
    public bool Counted { get; private set; }

    public void Begin(uint territory)
    {
        if (Active) return;
        Territory = territory;
        Confirmed = false;
        Counted = false;
    }

    public void Confirm(uint territory)
    {
        if (Active && territory == Territory) Confirmed = true;
    }

    public bool TryCount()
    {
        if (!Confirmed || Counted) return false;
        Counted = true;
        return true;
    }

    public void Reset()
    {
        Territory = 0;
        Confirmed = false;
        Counted = false;
    }
}
