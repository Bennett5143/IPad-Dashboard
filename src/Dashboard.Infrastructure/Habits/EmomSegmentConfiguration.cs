using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class EmomSegmentConfiguration : IEntityTypeConfiguration<EmomSegment>
{
    public void Configure(EntityTypeBuilder<EmomSegment> builder)
    {
        builder.HasKey(s => s.Id);
    }
}
