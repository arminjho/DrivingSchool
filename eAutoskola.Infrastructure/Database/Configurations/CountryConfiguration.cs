using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.Property(c => c.Code).HasMaxLength(2).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();
    }
}
