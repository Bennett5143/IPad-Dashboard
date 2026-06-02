public class EmomWorkout
{
    public int Id { get; set; }
    public int HabitEntryId { get; set; }
    public List<EmomSegment> Segments { get; set; } = new();

    public int TotalMinutes => Segments.Sum(s => s.MinuteCount);
    public int TotalPushups => Segments.Sum(s => s.MinuteCount * s.PushupsPerMinute);
    public int TotalPullups => Segments.Sum(s => s.MinuteCount * s.PullupsPerMinute);
}