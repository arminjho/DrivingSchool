using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class AnswerOptionConfiguration : IEntityTypeConfiguration<AnswerOption>
{
    public void Configure(EntityTypeBuilder<AnswerOption> builder)
    {
        builder.Property(a => a.Text).HasMaxLength(500).IsRequired();

        builder.HasOne(a => a.Question)
            .WithMany(q => q.AnswerOptions)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
