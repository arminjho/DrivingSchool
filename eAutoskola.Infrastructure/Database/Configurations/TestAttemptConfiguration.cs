using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class TestAttemptConfiguration : IEntityTypeConfiguration<TestAttempt>
{
    public void Configure(EntityTypeBuilder<TestAttempt> builder)
    {
        builder.HasOne(a => a.Student)
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Test)
            .WithMany()
            .HasForeignKey(a => a.TestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
