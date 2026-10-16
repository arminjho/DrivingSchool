using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
    public void Configure(EntityTypeBuilder<Charge> builder)
    {
        builder.Property(c => c.Amount).HasColumnType("decimal(10,2)");
        builder.Property(c => c.Description).HasMaxLength(250).IsRequired();

        builder.HasOne(c => c.Student)
            .WithMany()
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ServiceType)
            .WithMany()
            .HasForeignKey(c => c.ServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Lesson)
            .WithMany(l => l.Charges)
            .HasForeignKey(c => c.LessonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.StudentId);
        builder.HasIndex(c => c.LessonId);

        // Package charges (no lesson) exist once per student and service type - this makes the
        // lazy "create on first visit" provisioning safe under concurrent requests.
        builder.HasIndex(c => new { c.StudentId, c.ServiceTypeId })
            .IsUnique()
            .HasFilter("[LessonId] IS NULL");
    }
}
