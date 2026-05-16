namespace Dashboard.Domain.Entities;
public class Quote
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Author { get; set; }
}