using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class QuestionAreaConfiguration : IEntityTypeConfiguration<QuestionArea>
{
    public void Configure(EntityTypeBuilder<QuestionArea> builder)
    {
        builder.Property(a => a.Name).HasMaxLength(150).IsRequired();
    }
}
