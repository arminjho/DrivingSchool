using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Payments;

namespace eAutoskola.Infrastructure.Database.Seed;

/// <summary>
/// Paid history on top of the package charges that came with each student. These are historical
/// records: their PayPal ids start with SEED-, so they can never be captured or refunded through
/// PayPal - the real sandbox flow is tried with the unpaid extra-lesson charge of Adnan Zukić
/// (README, "Provjera plaćanja").
/// </summary>
internal static class SeedFinanceStep
{
    private const string PayPalCurrency = "EUR";

    public static async Task<List<(User Student, Payment Payment)>> RunAsync(
        EAutoskolaDbContext context, SeedReference reference, SeedPeople people, SeedLessons lessons, DateTime now, CancellationToken cancellationToken)
    {
        var serviceTypes = reference.ServiceTypes.ToDictionary(s => s.Code);
        var extraLesson = serviceTypes[ServiceTypeCodes.ExtraLesson];
        var packageCharges = context.Charges.Local.Where(c => c.LessonId is null).ToList();
        var payments = new List<(User, Payment)>();
        var orderNumber = 0;

        List<Installment> Installments(User student, string serviceTypeCode)
            => packageCharges
                .Single(c => ReferenceEquals(c.Student, student) && c.ServiceTypeId == serviceTypes[serviceTypeCode].Id)
                .Installments.OrderBy(i => i.Number).ToList();

        Payment NewPayment(Installment installment, PaymentStatus status, DateTime at)
        {
            orderNumber++;
            var amountEur = CurrencyConversion.KmToEur(installment.Amount);
            return new Payment
            {
                Amount = installment.Amount,
                PayPalAmount = amountEur,
                PayPalCurrency = PayPalCurrency,
                CapturedAmount = amountEur,
                Status = status,
                PayPalOrderId = $"SEED-ORDER-{orderNumber:D4}",
                PayPalApprovalUrl = string.Empty,
                PayPalCaptureId = $"SEED-CAPTURE-{orderNumber:D4}",
                CreatedAt = at,
                CompletedAt = at,
            };
        }

        void Pay(User student, Installment installment, DateTime paidAt)
        {
            installment.Status = InstallmentStatus.Paid;
            var payment = NewPayment(installment, PaymentStatus.Completed, paidAt < now ? paidAt : now.AddDays(-1));
            installment.Payments.Add(payment);
            payments.Add((student, payment));
        }

        // --- Package installments paid so far; the newest students (registered this month) have paid nothing ---
        var establishedCount = people.Students.Length - SeedFleetAndPeopleData.NewStudentCount;
        for (var s = 0; s < establishedCount; s++)
        {
            var student = people.Students[s];
            var registeredAt = student.CreatedAt;
            var theory = Installments(student, ServiceTypeCodes.TheoreticalTraining);
            var practical = Installments(student, ServiceTypeCodes.PracticalDriving);
            var administration = Installments(student, ServiceTypeCodes.AdministrationAndExam);

            var theoryPaid = s == 4 ? 3 : s == 0 ? 2 : 1 + s % 3;
            for (var n = 0; n < theoryPaid; n++)
            {
                Pay(student, theory[n], registeredAt.AddDays(10 + 25 * n));
            }

            var practicalPaid = s == 4 ? 3 : (s % 3 != 2 ? 1 : 0) + (s % 4 == 0 ? 1 : 0);
            for (var n = 0; n < practicalPaid; n++)
            {
                Pay(student, practical[n], registeredAt.AddDays(14 + 30 * n));
            }

            if (s == 4 || s % 5 == 0)
            {
                Pay(student, administration[0], registeredAt.AddDays(20));
            }
        }

        // --- Adnan Zukić: an extra lesson that was paid, then cancelled by the admin and refunded ---
        var adnan = people.Students[4];
        var refundedLesson = lessons.AdnanRefundedLesson;
        var paidAt = refundedLesson.StartsAtUtc.AddDays(-4);
        var refundedAt = refundedLesson.StartsAtUtc.AddDays(-1);

        var refundedInstallment = new Installment
        {
            Number = 1, Amount = extraLesson.Price, DueDate = refundedLesson.StartsAtUtc,
            Status = InstallmentStatus.Cancelled, CreatedAt = paidAt.AddDays(-1),
        };
        var refundedPayment = NewPayment(refundedInstallment, PaymentStatus.Refunded, paidAt);
        refundedPayment.Refunds.Add(new Refund
        {
            Amount = refundedPayment.PayPalAmount, AmountKm = extraLesson.Price, PayPalRefundId = "SEED-REFUND-0001",
            Status = RefundStatus.Completed, CreatedAt = refundedAt,
        });
        refundedInstallment.Payments.Add(refundedPayment);
        context.Charges.Add(new Charge
        {
            Student = adnan, ServiceTypeId = extraLesson.Id, Lesson = refundedLesson, Amount = extraLesson.Price,
            Description = $"{extraLesson.Name} ({refundedLesson.StartsAtUtc:dd.MM.yyyy.})",
            Status = ChargeStatus.Cancelled, CreatedAt = paidAt.AddDays(-1),
            Installments = [refundedInstallment],
        });

        // --- Adnan Zukić: the upcoming confirmed extra lesson whose charge is still UNPAID - the one to pay
        // with a real PayPal sandbox buyer and then cancel for a real refund ---
        var upcomingLesson = lessons.AdnanUpcomingLesson;
        context.Charges.Add(new Charge
        {
            Student = adnan, ServiceTypeId = extraLesson.Id, Lesson = upcomingLesson, Amount = extraLesson.Price,
            Description = $"{extraLesson.Name} ({upcomingLesson.StartsAtUtc:dd.MM.yyyy.})",
            Installments = [new Installment { Number = 1, Amount = extraLesson.Price, DueDate = upcomingLesson.StartsAtUtc }],
        });

        await context.SaveChangesAsync(cancellationToken);
        return payments;
    }
}
