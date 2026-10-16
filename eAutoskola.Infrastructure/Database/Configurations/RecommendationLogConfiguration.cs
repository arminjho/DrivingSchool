using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class RecommendationLogConfiguration : IEntityTypeConfiguration<RecommendationLog>
{
    public void Configure(EntityTypeBuilder<RecommendationLog> builder)
    {
        builder.Property(r => r.Explanation).HasMaxLength(1000).IsRequired();

        builder.HasOne(r => r.Student)
            .WithMany()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.StudyLesson)
            .WithMany()
            .HasForeignKey(r => r.StudyLessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
