using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class VehicleServiceTypeConfiguration : IEntityTypeConfiguration<VehicleServiceType>
{
    public void Configure(EntityTypeBuilder<VehicleServiceType> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
    }
}
