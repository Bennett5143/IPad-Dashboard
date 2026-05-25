namespace Dashboard.Domain.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
