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

        // Lauf-Details als Owned Entity → gleiche Tabelle, keine eigene
        builder.OwnsOne(h => h.Running, running =>
        {
            running.Property(d => d.DurationMinutes)
                .HasColumnName("DurationMinutes");
            running.Property(d => d.PaceMinPerKm)
                .HasColumnName("PaceMinPerKm")
                .HasPrecision(5, 2);
        });

        // EMOM-Workout: eigene Tabelle, 1:0..1, Cascade beim Löschen des Eintrags
        builder.HasOne(h => h.Emom)
            .WithOne()
            .HasForeignKey<EmomWorkout>(w => w.HabitEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => new { h.Date, h.Kind })
            .IsUnique();
    }
}