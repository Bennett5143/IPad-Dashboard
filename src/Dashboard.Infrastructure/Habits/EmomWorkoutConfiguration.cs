using Dashboard.Domain.Entities;                       
using Microsoft.EntityFrameworkCore;                    
using Microsoft.EntityFrameworkCore.Metadata.Builders;  

public class EmomWorkoutConfiguration : IEntityTypeConfiguration<EmomWorkout>
{
    public void Configure(EntityTypeBuilder<EmomWorkout> builder)
    {
        builder.HasKey(w => w.Id);

        builder.HasMany(w => w.Segments)
            .WithOne()
            .HasForeignKey(s => s.EmomWorkoutId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}