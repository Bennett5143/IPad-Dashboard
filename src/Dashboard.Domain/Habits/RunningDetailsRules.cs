public static class RunningDetailsRules
{
    public static string? Validate(int durationMinutes, decimal paceMinPerKm)
    {
        if (durationMinutes <= 0) return "Die Dauer muss größer als 0 sein.";
        if (paceMinPerKm <= 0) return "Die Pace muss größer als 0 sein.";
        return null;
    }
}
