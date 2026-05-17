using Dashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Persistence.Configurations;

public class HabitEntryConfiguration : IEntityTypeConfiguration<HabitEntry>
{
    public void Configure(EntityTypeBuilder<HabitEntry> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Kind)
            .HasConversion<string>()   // Enum als String in der DB, nicht als int
            .HasMaxLength(32);

        builder.Property(h => h.Date)
            .IsRequired();

        // RunningDetails als Owned Entity → gleiche Tabelle, keine eigene
        builder.OwnsOne(h => h.Details, details =>
        {
            details.Property(d => d.DurationMinutes)
                .HasColumnName("DurationMinutes");
            details.Property(d => d.PaceMinPerKm)
                .HasColumnName("PaceMinPerKm")
                .HasPrecision(5, 2);
        });

        builder.HasIndex(h => new { h.Date, h.Kind })
            .IsUnique();   // pro Tag und Habit-Typ max. ein Eintrag
    }
}
