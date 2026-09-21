namespace AutoPvpSeriesGrind.Core.Rotation;

internal readonly record struct MovementKit(uint[] GapClosers, uint[] ForwardDashes, uint[] AllyDashes);

internal static class MovementAssist
{
    private static readonly uint[] None = [];

    private static readonly Dictionary<uint, MovementKit> Kits = new()
    {
        [20] = new(GapClosers: [29484], ForwardDashes: None, AllyDashes: [29484]),
        [22] = new(GapClosers: [29493], ForwardDashes: None, AllyDashes: None),
        [25] = new(GapClosers: None, ForwardDashes: None, AllyDashes: [29660]),
        [27] = new(GapClosers: [29667], ForwardDashes: None, AllyDashes: None),
        [30] = new(GapClosers: [29513], ForwardDashes: [29513], AllyDashes: None),
        [33] = new(GapClosers: None, ForwardDashes: [41506], AllyDashes: None),
        [35] = new(GapClosers: [29699], ForwardDashes: None, AllyDashes: None),
        [38] = new(GapClosers: None, ForwardDashes: [29430], AllyDashes: None),
        [39] = new(GapClosers: None, ForwardDashes: [29550], AllyDashes: None),
        [40] = new(GapClosers: [29261], ForwardDashes: None, AllyDashes: [29261]),
        [41] = new(GapClosers: [39184], ForwardDashes: None, AllyDashes: [39184]),
        [42] = new(GapClosers: None, ForwardDashes: [39210], AllyDashes: None),
    };

    public static bool TryGet(uint jobId, out MovementKit kit) => Kits.TryGetValue(jobId, out kit);
}
