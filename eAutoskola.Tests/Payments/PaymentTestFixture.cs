using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Messaging;
using eAutoskola.Domain.Notifications;
using eAutoskola.Domain.Payments;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace eAutoskola.Tests.Payments;

/// <summary>An in-memory database plus mocked collaborators shared by the payment service tests.</summary>
public class PaymentTestFixture
{
    public const int StudentId = 1;
    public const int OtherStudentId = 2;
    public const int InstructorId = 3;

    public EAutoskolaDbContext Context { get; } = new(
        new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public Mock<IPayPalClient> PayPal { get; } = new();
    public Mock<IMessagePublisher> Publisher { get; } = new();
    public Mock<INotificationPusher> Pusher { get; } = new();
    public Mock<ICurrentUserService> CurrentUser { get; } = new();

    public PaymentTestFixture(int currentUserId = StudentId, string role = Role.Student)
    {
        CurrentUser.SetupGet(c => c.UserId).Returns(currentUserId);
        CurrentUser.SetupGet(c => c.Role).Returns(role);
        CurrentUser.Setup(c => c.IsInRole(It.IsAny<string>())).Returns((string r) => r == role);
        PayPal.SetupGet(p => p.Currency).Returns("EUR");
        Publisher.Setup(p => p.PublishAsync(It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Context.Users.AddRange(
            NewUser(StudentId, "student@test.ba", Role.Student),
            NewUser(OtherStudentId, "other@test.ba", Role.Student),
            NewUser(InstructorId, "instructor@test.ba", Role.Instructor));
        Context.SaveChanges();
    }

    /// <summary>The seeded price list: the three package items plus the extra-lesson price.</summary>
    public void AddPriceList()
    {
        Context.ServiceTypes.AddRange(
            new ServiceType { Id = 11, Code = ServiceTypeCodes.TheoreticalTraining, Name = "Teorijska obuka (20h)", Price = 200m },
            new ServiceType { Id = 12, Code = ServiceTypeCodes.PracticalDriving, Name = "Praktična vožnja (30h)", Price = 1200m },
            new ServiceType { Id = 13, Code = ServiceTypeCodes.AdministrationAndExam, Name = "Administracija i ispit", Price = 150m },
            new ServiceType { Id = 14, Code = ServiceTypeCodes.ExtraLesson, Name = "Dodatni čas vožnje", Price = 40m });
        Context.SaveChanges();
    }

    /// <summary>Counts SaveChanges calls so a test can prove several entities were stored in one.</summary>
    public Func<int> CountSaves()
    {
        var saves = 0;
        Context.SavedChanges += (_, _) => saves++;
        return () => saves;
    }

    private static User NewUser(int id, string email, string role) => new()
    {
        Id = id, Email = email, PasswordHash = "x", Role = role, FirstName = "Ime", LastName = "Prezime", PhoneNumber = "+387 61 000 000",
    };

    /// <summary>A student's extra-lesson charge with one pending installment (40 KM).</summary>
    public Installment AddInstallment(int studentId = StudentId, int? lessonId = null)
    {
        var charge = new Charge
        {
            StudentId = studentId, ServiceTypeId = 1, LessonId = lessonId, Amount = 40m, Description = "Dodatni čas vožnje",
            Installments = [new Installment { Number = 1, Amount = 40m, DueDate = DateTime.UtcNow.AddDays(5) }],
        };
        Context.Charges.Add(charge);
        Context.SaveChanges();
        return charge.Installments.Single();
    }

    public Payment AddPayment(Installment installment, PaymentStatus status, DateTime? createdAtUtc = null, string orderId = "ORDER-1")
    {
        var payment = new Payment
        {
            InstallmentId = installment.Id, Amount = installment.Amount, PayPalAmount = 20.45m, PayPalCurrency = "EUR",
            PayPalOrderId = orderId, PayPalApprovalUrl = $"https://paypal.test/approve/{orderId}",
            Status = status, CreatedAt = createdAtUtc ?? DateTime.UtcNow,
        };
        if (status is PaymentStatus.Completed or PaymentStatus.Refunded)
        {
            payment.CapturedAmount = 20.45m;
            payment.PayPalCaptureId = "CAPTURE-1";
            payment.CompletedAt = payment.CreatedAt;
            installment.Status = InstallmentStatus.Paid;
        }
        Context.Payments.Add(payment);
        Context.SaveChanges();
        return payment;
    }

    public Lesson AddLesson(DateTime startsAtUtc, LessonStatus status = LessonStatus.Confirmed, int studentId = StudentId)
    {
        var lesson = new Lesson
        {
            StudentId = studentId, InstructorId = InstructorId, DrivingZoneId = 1,
            StartsAtUtc = startsAtUtc, EndsAtUtc = startsAtUtc.AddHours(1), Status = status,
        };
        Context.Lessons.Add(lesson);
        Context.SaveChanges();
        return lesson;
    }
}
