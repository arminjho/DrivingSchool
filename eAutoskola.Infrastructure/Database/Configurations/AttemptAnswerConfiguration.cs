using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class AttemptAnswerConfiguration : IEntityTypeConfiguration<AttemptAnswer>
{
    public void Configure(EntityTypeBuilder<AttemptAnswer> builder)
    {
        builder.HasOne(a => a.TestAttempt)
            .WithMany(t => t.Answers)
            .HasForeignKey(a => a.TestAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Question)
            .WithMany()
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.SelectedAnswerOption)
            .WithMany()
            .HasForeignKey(a => a.SelectedAnswerOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
