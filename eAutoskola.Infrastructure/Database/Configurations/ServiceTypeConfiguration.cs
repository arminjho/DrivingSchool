using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.Property(s => s.Code).HasMaxLength(40).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Price).HasColumnType("decimal(10,2)");
        builder.HasIndex(s => s.Code).IsUnique();
    }
}
