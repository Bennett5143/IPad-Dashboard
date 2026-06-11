using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Habits;

internal sealed class EmomSegmentConfiguration : IEntityTypeConfiguration<EmomSegment>
{
    public void Configure(EntityTypeBuilder<EmomSegment> builder)
    {
        builder.HasKey(s => s.Id);
    }
}
