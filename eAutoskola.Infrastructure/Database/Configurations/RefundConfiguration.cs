using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.Property(r => r.Amount).HasColumnType("decimal(10,2)");
        builder.Property(r => r.AmountKm).HasColumnType("decimal(10,2)");
        builder.Property(r => r.PayPalRefundId).HasMaxLength(64);

        builder.HasOne(r => r.Payment)
            .WithMany(p => p.Refunds)
            .HasForeignKey(r => r.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.PaymentId);
    }
}
