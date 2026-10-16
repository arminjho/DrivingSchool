using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class StudentStudyLessonConfiguration : IEntityTypeConfiguration<StudentStudyLesson>
{
    public void Configure(EntityTypeBuilder<StudentStudyLesson> builder)
    {
        builder.HasOne(s => s.Student)
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.StudyLesson)
            .WithMany()
            .HasForeignKey(s => s.StudyLessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.StudentId, s.StudyLessonId }).IsUnique();
    }
}
