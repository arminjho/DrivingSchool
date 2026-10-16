using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eAutoskola.Infrastructure.Database.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.Amount).HasColumnType("decimal(10,2)");
        builder.Property(p => p.PayPalAmount).HasColumnType("decimal(10,2)");
        builder.Property(p => p.CapturedAmount).HasColumnType("decimal(10,2)");
        builder.Property(p => p.PayPalCurrency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.PayPalOrderId).HasMaxLength(64).IsRequired();
        builder.Property(p => p.PayPalApprovalUrl).HasMaxLength(500).IsRequired();
        builder.Property(p => p.PayPalCaptureId).HasMaxLength(64);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasOne(p => p.Installment)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InstallmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.PayPalOrderId).IsUnique();
        builder.HasIndex(p => p.InstallmentId);
    }
}
