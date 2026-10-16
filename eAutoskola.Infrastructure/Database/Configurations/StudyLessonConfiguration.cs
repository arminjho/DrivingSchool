using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class StudyLessonConfiguration : IEntityTypeConfiguration<StudyLesson>
{
    public void Configure(EntityTypeBuilder<StudyLesson> builder)
    {
        builder.Property(l => l.Title).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Tags).HasMaxLength(500).IsRequired();
        builder.Property(l => l.ChapterLabel).HasMaxLength(50).IsRequired();

        builder.HasOne(l => l.QuestionArea)
            .WithMany()
            .HasForeignKey(l => l.QuestionAreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
