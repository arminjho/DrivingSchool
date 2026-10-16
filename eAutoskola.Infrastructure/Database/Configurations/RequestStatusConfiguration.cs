using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class RequestStatusConfiguration : IEntityTypeConfiguration<RequestStatus>
{
    public void Configure(EntityTypeBuilder<RequestStatus> builder)
    {
        builder.Property(s => s.Code).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(300);
        builder.HasIndex(s => s.Code).IsUnique();
    }
}
