using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.Property(i => i.Amount).HasColumnType("decimal(10,2)");

        builder.HasOne(i => i.Charge)
            .WithMany(c => c.Installments)
            .HasForeignKey(i => i.ChargeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.ChargeId, i.Number }).IsUnique();
    }
}
