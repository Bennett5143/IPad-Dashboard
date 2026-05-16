using Dashboard.Domain.Enums;
using Dashboard.Domain.ValueObjects;

namespace Dashboard.Domain.Entities;
public class HabitEntry
{
    public int Id { get; set; }
    public HabitKind Kind { get; set; } 
    public DateOnly Date { get; set; } 
    public RunningDetails? Details { get; set; } 
}