using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.HasOne(p => p.User)
            .WithOne(u => u.StudentProfile)
            .HasForeignKey<StudentProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.UserId).IsUnique();

        builder.HasOne(p => p.VehicleCategory)
            .WithMany()
            .HasForeignKey(p => p.VehicleCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
