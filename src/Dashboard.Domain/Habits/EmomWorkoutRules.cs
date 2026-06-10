namespace Dashboard.Domain.Habits;

public static class EmomWorkoutRules
{
    /// <summary>Gibt null zurück wenn valide, sonst eine Fehlerbeschreibung.</summary>
    public static string? ValidateSegments(IReadOnlyList<EmomSegment> segments)
    {
        if (segments.Count == 0)
            return "Ein EMOM braucht mindestens ein Segment.";

        var ordered = segments.OrderBy(s => s.FromMinute).ToList();

        if (ordered[0].FromMinute != 1)
            return "Das erste Segment muss bei Minute 1 beginnen.";

        for (var i = 0; i < ordered.Count; i++)
        {
            var seg = ordered[i];
            if (seg.FromMinute > seg.ToMinute)
                return $"Segment {i + 1}: Beginn liegt nach dem Ende.";
            if (seg.PushupsPerMinute < 0 || seg.PullupsPerMinute < 0)
                return $"Segment {i + 1}: Reps dürfen nicht negativ sein.";
            if (i > 0 && seg.FromMinute != ordered[i - 1].ToMinute + 1)
                return $"Lücke oder Überschneidung vor Segment {i + 1}.";
        }
        return null;
    }
}
