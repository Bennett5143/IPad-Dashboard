namespace Dashboard.Domain.Habitentry;
public class Habitentry
{
    public int Id { get; set; }
    public HabitKind Kind { get; set; } 
    public DateOnly Date { get; set; } 
    public RunningDetails? Details { get; set; } 
}