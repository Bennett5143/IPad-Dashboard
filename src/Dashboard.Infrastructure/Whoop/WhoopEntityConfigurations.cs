using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard.Infrastructure.Whoop;

internal sealed class WhoopTokenEntityConfiguration : IEntityTypeConfiguration<WhoopTokenEntity>
{
    public void Configure(EntityTypeBuilder<WhoopTokenEntity> builder)
    {
        builder.ToTable("WhoopTokens");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        // WHOOP-Tokens sind JWTs und können lang sein → bewusst unbeschränkt (text), nicht limitieren.
    }
}
