using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class LessonStatusHistoryConfiguration : IEntityTypeConfiguration<LessonStatusHistory>
{
    public void Configure(EntityTypeBuilder<LessonStatusHistory> builder)
    {
        builder.Property(h => h.Reason).HasMaxLength(1000);

        builder.HasOne(h => h.Lesson)
            .WithMany(l => l.StatusHistory)
            .HasForeignKey(h => h.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
