using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class VehicleServiceConfiguration : IEntityTypeConfiguration<VehicleService>
{
    public void Configure(EntityTypeBuilder<VehicleService> builder)
    {
        builder.Property(s => s.Description).HasMaxLength(500).IsRequired();
        builder.Property(s => s.Cost).HasColumnType("decimal(10,2)");

        builder.HasOne(s => s.Vehicle)
            .WithMany(v => v.Services)
            .HasForeignKey(s => s.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.VehicleServiceType)
            .WithMany()
            .HasForeignKey(s => s.VehicleServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
