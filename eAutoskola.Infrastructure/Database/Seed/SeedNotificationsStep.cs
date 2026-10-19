using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Payments;
using eAutoskola.Domain.Resources;

namespace eAutoskola.Infrastructure.Database.Seed;

/// <summary>
/// The in-app notifications that the seeded lessons and payments would have caused - same
/// titles and texts as the services use (Messages), recent events first, a mix of read and
/// unread so the "označi pročitano" flow can be tried.
/// </summary>
internal static class SeedNotificationsStep
{
    public static async Task RunAsync(
        EAutoskolaDbContext context, SeedReference reference, SeedPeople people, SeedLessons lessons, List<(User Student, Payment Payment)> payments, DateTime now, CancellationToken cancellationToken)
    {
        var notifications = new List<Notification>();
        var counter = 0;

        void Add(User user, string title, string message, NotificationType type, DateTime at)
        {
            at = at < now ? at : now.AddMinutes(-10);
            notifications.Add(new Notification
            {
                User = user, Title = title, Message = message, Type = type, CreatedAt = at,
                IsRead = at < now.AddDays(-4) || ++counter % 3 == 0,
            });
        }

        static string FullName(User user) => $"{user.FirstName} {user.LastName}";

        // Instructors: requests waiting for an answer.
        foreach (var lesson in lessons.All.Where(l => l.Status == LessonStatus.Pending).OrderByDescending(l => l.CreatedAt).Take(10))
        {
            Add(lesson.Instructor!, Messages.NotificationLessonRequestedTitle,
                string.Format(Messages.NotificationLessonRequestedBody, FullName(lesson.Student!)), NotificationType.LessonCreated, lesson.CreatedAt);
        }

        // Students: confirmed, finished and cancelled lessons.
        foreach (var lesson in lessons.All.Where(l => l.Status == LessonStatus.Confirmed && l.StartsAtUtc > now).OrderByDescending(l => l.CreatedAt).Take(10))
        {
            Add(lesson.Student!, Messages.NotificationLessonConfirmedTitle, Messages.NotificationLessonConfirmedBody,
                NotificationType.LessonConfirmed, lesson.CreatedAt.AddDays(1));
        }

        foreach (var lesson in lessons.All.Where(l => l.Status == LessonStatus.Completed).OrderByDescending(l => l.CompletedAt).Take(12))
        {
            Add(lesson.Student!, Messages.NotificationLessonCompletedTitle, Messages.NotificationLessonCompletedBody,
                NotificationType.LessonCompleted, lesson.CompletedAt!.Value);
        }

        foreach (var lesson in lessons.All.Where(l => l.Status == LessonStatus.Cancelled).OrderByDescending(l => l.StartsAtUtc).Take(6))
        {
            Add(lesson.Student!, Messages.NotificationLessonRejectedTitle,
                string.Format(Messages.NotificationLessonRejectedBody, lesson.RejectionReason), NotificationType.LessonRejected, lesson.CreatedAt.AddDays(2));
        }

        // Payments received.
        foreach (var (student, payment) in payments.OrderByDescending(p => p.Payment.CompletedAt).Take(8))
        {
            Add(student, Messages.NotificationPaymentReceivedTitle,
                string.Format(Messages.NotificationPaymentReceivedBody, payment.Amount), NotificationType.PaymentReceived, payment.CompletedAt!.Value);
        }

        // Administrators: what happened in the school lately - new requests, cancellations, payments.
        foreach (var admin in people.Admins)
        {
            foreach (var lesson in lessons.All.Where(l => l.Status == LessonStatus.Pending).OrderByDescending(l => l.CreatedAt).Take(4))
            {
                Add(admin, Messages.NotificationLessonRequestedTitle,
                    string.Format(Messages.NotificationLessonRequestedBody, FullName(lesson.Student!)), NotificationType.LessonCreated, lesson.CreatedAt);
            }

            foreach (var lesson in lessons.All.Where(l => l.Status == LessonStatus.Cancelled).OrderByDescending(l => l.StartsAtUtc).Take(2))
            {
                Add(admin, Messages.NotificationLessonCancelledTitle,
                    string.Format(Messages.NotificationAdminLessonCancelledBody, FullName(lesson.Student!)), NotificationType.LessonCancelled, lesson.CreatedAt.AddDays(2));
            }

            foreach (var (student, payment) in payments.OrderByDescending(p => p.Payment.CompletedAt).Take(3))
            {
                Add(admin, Messages.NotificationPaymentReceivedTitle,
                    string.Format(Messages.NotificationAdminPaymentReceivedBody, FullName(student), payment.Amount), NotificationType.PaymentReceived, payment.CompletedAt!.Value);
            }
        }

        // The extra lesson Adnan still has to pay.
        var extraLessonPrice = reference.ServiceTypes.Single(s => s.Code == ServiceTypeCodes.ExtraLesson).Price;
        Add(lessons.AdnanUpcomingLesson.Student!, Messages.NotificationLessonConfirmedTitle,
            string.Format(Messages.NotificationLessonConfirmedExtraBody, extraLessonPrice), NotificationType.LessonConfirmed, lessons.AdnanUpcomingLesson.CreatedAt.AddDays(1));

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync(cancellationToken);
    }
}
