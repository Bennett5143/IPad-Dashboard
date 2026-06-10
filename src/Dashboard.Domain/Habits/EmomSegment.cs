public class EmomSegment
{
    public int Id { get; set; }
    public int EmomWorkoutId { get; set; }
    public int FromMinute { get; set; }          // inklusiv, 1-basiert
    public int ToMinute { get; set; }            // inklusiv
    public int PushupsPerMinute { get; set; }
    public int PullupsPerMinute { get; set; }

    public int MinuteCount => ToMinute - FromMinute + 1;
}
