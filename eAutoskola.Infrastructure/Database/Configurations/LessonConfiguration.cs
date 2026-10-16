using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.Property(l => l.Note).HasMaxLength(1000);
        builder.Property(l => l.RejectionReason).HasMaxLength(1000);
        builder.Property(l => l.InstructorNotes).HasMaxLength(2000);

        builder.HasOne(l => l.Student)
            .WithMany()
            .HasForeignKey(l => l.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Instructor)
            .WithMany()
            .HasForeignKey(l => l.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Vehicle)
            .WithMany()
            .HasForeignKey(l => l.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.DrivingZone)
            .WithMany()
            .HasForeignKey(l => l.DrivingZoneId)
            .OnDelete(DeleteBehavior.Restrict);

        // Defense-in-depth against the exact-same-slot race; general interval overlap is
        // checked in the service layer (a unique index can't express interval overlap).
        builder.HasIndex(l => new { l.InstructorId, l.StartsAtUtc })
            .IsUnique()
            .HasFilter($"[Status] IN ({(int)LessonStatus.Pending}, {(int)LessonStatus.Confirmed})");
    }
}
