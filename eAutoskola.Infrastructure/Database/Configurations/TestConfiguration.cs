using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class TestConfiguration : IEntityTypeConfiguration<Test>
{
    public void Configure(EntityTypeBuilder<Test> builder)
    {
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();

        builder.HasOne(t => t.QuestionArea)
            .WithMany()
            .HasForeignKey(t => t.QuestionAreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
