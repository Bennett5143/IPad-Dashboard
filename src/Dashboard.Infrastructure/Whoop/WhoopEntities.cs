namespace Dashboard.Infrastructure.Whoop;

/// <summary>Single-Row-Entity (Id = 1) mit dem aktuellen WHOOP-OAuth-Token-Satz.</summary>
internal sealed class WhoopTokenEntity
{
    public int Id { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
