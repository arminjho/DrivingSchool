using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.PostalCode).HasMaxLength(10).IsRequired();

        builder.HasOne(c => c.Country)
            .WithMany(co => co.Cities)
            .HasForeignKey(c => c.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
