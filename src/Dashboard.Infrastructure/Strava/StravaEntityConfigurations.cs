using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Strava;

internal sealed class RunActivityEntityConfiguration : IEntityTypeConfiguration<RunActivityEntity>
{
    public void Configure(EntityTypeBuilder<RunActivityEntity> builder)
    {
        builder.ToTable("RunActivities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();           // Strava liefert die Id
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Type).HasMaxLength(32);
        builder.Property(e => e.Route).HasColumnType("geometry(LineString, 4326)");
        builder.HasIndex(e => e.StartUtc);
    }
}

internal sealed class StravaTokenEntityConfiguration : IEntityTypeConfiguration<StravaTokenEntity>
{
    public void Configure(EntityTypeBuilder<StravaTokenEntity> builder)
    {
        builder.ToTable("StravaTokens");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.AccessToken).HasMaxLength(256);
        builder.Property(e => e.RefreshToken).HasMaxLength(256);
    }
}

internal sealed class SyncStateEntityConfiguration : IEntityTypeConfiguration<SyncStateEntity>
{
    public void Configure(EntityTypeBuilder<SyncStateEntity> builder)
    {
        builder.ToTable("SyncStates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.LastError).HasMaxLength(1000);
    }
}
