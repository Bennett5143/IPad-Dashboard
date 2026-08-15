using Dashboard.Domain.Enums;

namespace Dashboard.Domain.Habits;

/// <summary>
/// Welche Gewohnheiten der Tracker führt. Eine Stelle für alle Ansichten und Zähler, damit die
/// Zahlen zusammenpassen.
/// <para>
/// Abgewählte Gewohnheiten bleiben absichtlich in <see cref="HabitKind"/>: der Wert liegt als
/// String in der Datenbank, und bestehende Zeilen (etwa <c>JumpRope</c>) müssen weiter lesbar sein.
/// Sie tauchen nur nirgends mehr auf — das kostet keine Datenwanderung und verliert keine Historie.
/// </para>
/// </summary>
public static class HabitCatalog
{
    /// <summary>Die geführten Gewohnheiten, in Anzeigereihenfolge.</summary>
    public static readonly IReadOnlyList<HabitKind> Active =
    [
        HabitKind.Strength,
        HabitKind.Zone2Run,
        HabitKind.Vo2MaxIntervals,
    ];

    /// <summary>Wird diese Gewohnheit noch geführt?</summary>
    public static bool IsActive(HabitKind kind) => Active.Contains(kind);
}
