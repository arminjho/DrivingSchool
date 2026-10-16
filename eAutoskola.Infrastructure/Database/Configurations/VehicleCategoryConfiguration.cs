using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class VehicleCategoryConfiguration : IEntityTypeConfiguration<VehicleCategory>
{
    public void Configure(EntityTypeBuilder<VehicleCategory> builder)
    {
        builder.Property(c => c.Code).HasMaxLength(1).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(300);
        builder.HasIndex(c => c.Code).IsUnique();
    }
}
