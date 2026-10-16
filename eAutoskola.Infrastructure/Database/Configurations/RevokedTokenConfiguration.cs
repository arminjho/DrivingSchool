using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class RevokedTokenConfiguration : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.Property(t => t.Jti).HasMaxLength(100).IsRequired();
        builder.HasIndex(t => t.Jti).IsUnique();
    }
}
