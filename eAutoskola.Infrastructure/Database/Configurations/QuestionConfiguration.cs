using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.Property(q => q.Text).HasMaxLength(1000).IsRequired();
        builder.Property(q => q.ImagePath).HasMaxLength(500);

        builder.HasOne(q => q.QuestionArea)
            .WithMany()
            .HasForeignKey(q => q.QuestionAreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.Test)
            .WithMany(t => t.Questions)
            .HasForeignKey(q => q.TestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
