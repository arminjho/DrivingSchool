using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.Property(v => v.RegistrationNumber).HasMaxLength(20).IsRequired();
        builder.Property(v => v.Make).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(100).IsRequired();
        builder.Property(v => v.ImagePath).HasMaxLength(500);

        builder.HasIndex(v => v.RegistrationNumber).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasOne(v => v.VehicleCategory)
            .WithMany()
            .HasForeignKey(v => v.VehicleCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
