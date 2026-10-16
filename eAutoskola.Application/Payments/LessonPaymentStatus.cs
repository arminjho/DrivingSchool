using eAutoskola.Application.Lessons;
using eAutoskola.Domain.Enums;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Payments;

public static class LessonPaymentStatus
{
    /// <summary>Fills RequiresPayment/IsPaid on already-mapped lesson DTOs with one query.
    /// Only lessons with an active (not refunded/cancelled) charge require payment.</summary>
    public static async Task ApplyAsync(EAutoskolaDbContext context, IReadOnlyCollection<LessonDto> lessons, CancellationToken cancellationToken)
    {
        if (lessons.Count == 0)
        {
            return;
        }

        var lessonIds = lessons.Select(l => l.Id).ToList();
        var charges = await context.Charges.AsNoTracking()
            .Where(c => c.LessonId != null && lessonIds.Contains(c.LessonId.Value) && c.Status == ChargeStatus.Active)
            .Select(c => new { LessonId = c.LessonId!.Value, IsPaid = c.Installments.Any(i => i.Status == InstallmentStatus.Paid) })
            .ToListAsync(cancellationToken);

        foreach (var charge in charges)
        {
            foreach (var lesson in lessons.Where(l => l.Id == charge.LessonId))
            {
                lesson.RequiresPayment = true;
                lesson.IsPaid = charge.IsPaid;
            }
        }
    }
}
