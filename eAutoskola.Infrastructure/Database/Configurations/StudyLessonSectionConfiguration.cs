using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class StudyLessonSectionConfiguration : IEntityTypeConfiguration<StudyLessonSection>
{
    public void Configure(EntityTypeBuilder<StudyLessonSection> builder)
    {
        builder.Property(s => s.Title).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Content).IsRequired();

        builder.HasOne(s => s.StudyLesson)
            .WithMany(l => l.Sections)
            .HasForeignKey(s => s.StudyLessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
