using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class InstructorProfileConfiguration : IEntityTypeConfiguration<InstructorProfile>
{
    public void Configure(EntityTypeBuilder<InstructorProfile> builder)
    {
        builder.HasOne(p => p.User)
            .WithOne(u => u.InstructorProfile)
            .HasForeignKey<InstructorProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.UserId).IsUnique();

        builder.HasOne(p => p.VehicleCategory)
            .WithMany()
            .HasForeignKey(p => p.VehicleCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
