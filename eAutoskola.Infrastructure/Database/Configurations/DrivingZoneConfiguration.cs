using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class DrivingZoneConfiguration : IEntityTypeConfiguration<DrivingZone>
{
    public void Configure(EntityTypeBuilder<DrivingZone> builder)
    {
        builder.Property(z => z.Name).HasMaxLength(100).IsRequired();
    }
}
