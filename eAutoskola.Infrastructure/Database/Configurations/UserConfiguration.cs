using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(30).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(u => u.Address).HasMaxLength(300);
        builder.Property(u => u.ProfileImagePath).HasMaxLength(500);

        // Filtered unique index: a soft-deleted user's email can be reused.
        builder.HasIndex(u => u.Email).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasOne(u => u.City)
            .WithMany()
            .HasForeignKey(u => u.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
