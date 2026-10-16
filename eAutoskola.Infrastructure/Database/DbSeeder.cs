using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Payments;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Payments;
using eAutoskola.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Infrastructure.Database;

/// <summary>
/// Idempotent runtime seeder (not EF HasData, so PasswordHasher - the same one AuthController
/// will use - can be called directly; this is how seed and runtime hashes stay in the same
/// format, per upute.md's explicit warning against mixing them). Values mirror
/// docs/prijava.md's mockups where possible so the eventual UI matches the seeded data.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(EAutoskolaDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Countries.AnyAsync(cancellationToken))
        {
            return; // already seeded
        }

        // --- Reference data ---
        var countries = new[]
        {
            new Country { Code = "BA", Name = "Bosna i Hercegovina" },
            new Country { Code = "HR", Name = "Hrvatska" },
            new Country { Code = "RS", Name = "Srbija" },
        };
        context.Countries.AddRange(countries);
        await context.SaveChangesAsync(cancellationToken);

        var ba = countries[0];
        var cities = new[]
        {
            new City { Name = "Sarajevo", PostalCode = "71000", CountryId = ba.Id },
            new City { Name = "Mostar", PostalCode = "88000", CountryId = ba.Id },
            new City { Name = "Banja Luka", PostalCode = "78000", CountryId = ba.Id },
            new City { Name = "Tuzla", PostalCode = "75000", CountryId = ba.Id },
            new City { Name = "Zenica", PostalCode = "72000", CountryId = ba.Id },
            new City { Name = "Bijeljina", PostalCode = "76300", CountryId = ba.Id },
        };
        context.Cities.AddRange(cities);

        var categories = new[]
        {
            new VehicleCategory { Code = "A", Name = "Motocikli", Description = "Vozila kategorije A - motocikli svih vrsta" },
            new VehicleCategory { Code = "B", Name = "Osobna vozila", Description = "Osobni automobili do 3.5t i 8 mjesta" },
            new VehicleCategory { Code = "C", Name = "Teretna vozila", Description = "Teretna vozila iznad 3.5t" },
            new VehicleCategory { Code = "D", Name = "Autobusi", Description = "Autobusi za prijevoz više od 8 putnika" },
        };
        context.VehicleCategories.AddRange(categories);

        var requestStatuses = new[]
        {
            new RequestStatus { Code = nameof(LessonStatus.Pending), Name = "Na čekanju", Description = "Zahtjev čeka odgovor instruktora" },
            new RequestStatus { Code = nameof(LessonStatus.Confirmed), Name = "Potvrđen", Description = "Instruktor je potvrdio termin" },
            new RequestStatus { Code = nameof(LessonStatus.Cancelled), Name = "Otkazan", Description = "Zahtjev je otkazan ili odbijen" },
            new RequestStatus { Code = nameof(LessonStatus.Completed), Name = "Završen", Description = "Čas je odrađen" },
        };
        context.RequestStatuses.AddRange(requestStatuses);

        var serviceTypes = new[]
        {
            new ServiceType { Code = ServiceTypeCodes.TheoreticalTraining, Name = "Teorijska obuka (20h)", Price = 200.00m },
            new ServiceType { Code = ServiceTypeCodes.PracticalDriving, Name = "Praktična vožnja (30h)", Price = 1200.00m },
            new ServiceType { Code = ServiceTypeCodes.AdministrationAndExam, Name = "Administracija i ispit", Price = 150.00m },
            new ServiceType { Code = ServiceTypeCodes.ExtraLesson, Name = "Dodatni čas vožnje", Price = 40.00m },
        };
        context.ServiceTypes.AddRange(serviceTypes);

        var vehicleServiceTypes = new[]
        {
            new VehicleServiceType { Name = "Redovni servis" },
            new VehicleServiceType { Name = "Zamjena ulja" },
            new VehicleServiceType { Name = "Tehnički pregled" },
        };
        context.VehicleServiceTypes.AddRange(vehicleServiceTypes);

        var zones = new[]
        {
            new DrivingZone { Name = "Centar" },
            new DrivingZone { Name = "Stari Grad" },
            new DrivingZone { Name = "Novi Grad" },
        };
        context.DrivingZones.AddRange(zones);

        var questionAreas = new[]
        {
            new QuestionArea { Name = "Saobraćajni znakovi" },
            new QuestionArea { Name = "Pravila vožnje" },
            new QuestionArea { Name = "Situacije u saobraćaju" },
            new QuestionArea { Name = "Prva pomoć" },
            new QuestionArea { Name = "Održavanje vozila" },
        };
        context.QuestionAreas.AddRange(questionAreas);

        await context.SaveChangesAsync(cancellationToken);

        var categoryB = categories.Single(c => c.Code == "B");
        var sarajevo = cities[0];
        var mostar = cities[1];

        // --- Users: the 3 required demo accounts (upute.md §5) + a handful more for testing ---
        var demoPasswordHash = PasswordHasher.Hash("test");

        var admin = new User
        {
            Email = "desktop", PasswordHash = demoPasswordHash, Role = Role.Admin,
            FirstName = "Admin", LastName = "Korisnik", PhoneNumber = "+387 61 000 000",
            CityId = sarajevo.Id,
        };
        var demoStudent = new User
        {
            Email = "mobile", PasswordHash = demoPasswordHash, Role = Role.Student,
            FirstName = "Mobile", LastName = "Učenik", PhoneNumber = "+387 61 111 111",
            CityId = sarajevo.Id,
        };
        var demoInstructor = new User
        {
            Email = "instruktor", PasswordHash = demoPasswordHash, Role = Role.Instructor,
            FirstName = "Instruktor", LastName = "Test", PhoneNumber = "+387 61 222 222",
            CityId = sarajevo.Id,
        };

        var studentUsers = new[]
        {
            demoStudent,
            new User { Email = "amina.hodzic@eautoskola.ba", PasswordHash = demoPasswordHash, Role = Role.Student, FirstName = "Amina", LastName = "Hodžić", PhoneNumber = "+387 61 123 456", CityId = sarajevo.Id },
            new User { Email = "emir.mustafic@eautoskola.ba", PasswordHash = demoPasswordHash, Role = Role.Student, FirstName = "Emir", LastName = "Mustafić", PhoneNumber = "+387 61 234 567", CityId = mostar.Id },
            new User { Email = "lejla.basic@eautoskola.ba", PasswordHash = demoPasswordHash, Role = Role.Student, FirstName = "Lejla", LastName = "Bašić", PhoneNumber = "+387 61 345 678", CityId = sarajevo.Id },
            new User { Email = "adnan.zukic@eautoskola.ba", PasswordHash = demoPasswordHash, Role = Role.Student, FirstName = "Adnan", LastName = "Zukić", PhoneNumber = "+387 61 456 789", CityId = mostar.Id },
        };

        var instructorUsers = new[]
        {
            demoInstructor,
            new User { Email = "damir.kovacevic@eautoskola.ba", PasswordHash = demoPasswordHash, Role = Role.Instructor, FirstName = "Damir", LastName = "Kovačević", PhoneNumber = "+387 61 654 321", CityId = sarajevo.Id },
            new User { Email = "sanja.peric@eautoskola.ba", PasswordHash = demoPasswordHash, Role = Role.Instructor, FirstName = "Sanja", LastName = "Perić", PhoneNumber = "+387 61 765 432", CityId = mostar.Id },
        };

        context.Users.Add(admin);
        context.Users.AddRange(studentUsers);
        context.Users.AddRange(instructorUsers);
        await context.SaveChangesAsync(cancellationToken);

        context.StudentProfiles.AddRange(studentUsers.Select(u => new StudentProfile
        {
            UserId = u.Id, VehicleCategoryId = categoryB.Id, RegisteredAt = DateTime.UtcNow,
        }));
        context.InstructorProfiles.AddRange(instructorUsers.Select(u => new InstructorProfile
        {
            UserId = u.Id, VehicleCategoryId = categoryB.Id,
        }));

        // Same method self-registration and the admin "Novi korisnik" use.
        foreach (var student in studentUsers)
        {
            await PackageChargeProvisioner.AddPackageChargesAsync(context, student, cancellationToken);
        }
        await context.SaveChangesAsync(cancellationToken);

        // --- Vehicles (same registrations as the docs/prijava.md mockup) ---
        var categoryC = categories.Single(c => c.Code == "C");
        var categoryD = categories.Single(c => c.Code == "D");

        var vehicles = new[]
        {
            new Vehicle { RegistrationNumber = "E22-A-123", Make = "VW", Model = "Polo", VehicleCategoryId = categoryB.Id, ManufactureYear = 2021, Status = VehicleStatus.Active },
            new Vehicle { RegistrationNumber = "E54-K-456", Make = "Škoda", Model = "Octavia", VehicleCategoryId = categoryB.Id, ManufactureYear = 2020, Status = VehicleStatus.Active },
            new Vehicle { RegistrationNumber = "E88-M-789", Make = "Ford", Model = "Focus", VehicleCategoryId = categoryB.Id, ManufactureYear = 2019, Status = VehicleStatus.InService },
            new Vehicle { RegistrationNumber = "E31-T-321", Make = "Mercedes", Model = "Sprinter", VehicleCategoryId = categoryC.Id, ManufactureYear = 2022, Status = VehicleStatus.Active },
            new Vehicle { RegistrationNumber = "E33-B-654", Make = "VW", Model = "Crafter", VehicleCategoryId = categoryD.Id, ManufactureYear = 2021, Status = VehicleStatus.Active },
        };
        context.Vehicles.AddRange(vehicles);
        await context.SaveChangesAsync(cancellationToken);

        var polo = vehicles[0];
        var octavia = vehicles[1];
        var focus = vehicles[2];
        var redovniServis = vehicleServiceTypes[0];
        var zamjenaUlja = vehicleServiceTypes[1];
        var tehnickiPregled = vehicleServiceTypes[2];

        context.VehicleServices.AddRange(
            new VehicleService { VehicleId = polo.Id, ServiceDate = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc), VehicleServiceTypeId = redovniServis.Id, Description = "Redovni servis - zamjena filtera i tekućina", Cost = 120.00m, Status = VehicleServiceStatus.Completed, CreatedAt = DateTime.UtcNow.AddDays(-6) },
            new VehicleService { VehicleId = focus.Id, ServiceDate = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc), VehicleServiceTypeId = zamjenaUlja.Id, Description = "Zamjena motornog ulja i filtera", Cost = 60.00m, Status = VehicleServiceStatus.InProgress, CreatedAt = DateTime.UtcNow.AddDays(-4) },
            new VehicleService { VehicleId = octavia.Id, ServiceDate = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc), VehicleServiceTypeId = tehnickiPregled.Id, Description = "Godišnji tehnički pregled", Cost = 80.00m, Status = VehicleServiceStatus.Scheduled, CreatedAt = DateTime.UtcNow.AddDays(-2) });

        // --- A handful of sample lessons across every status, for testing list/filter endpoints ---
        var centar = zones[0];
        var staroGrad = zones[1];
        var amina = studentUsers[1];
        var emir = studentUsers[2];
        var lejla = studentUsers[3];
        var damir = instructorUsers[1];
        var sanja = instructorUsers[2];

        var now = DateTime.UtcNow;

        var pendingLesson = new Lesson
        {
            StudentId = amina.Id, InstructorId = damir.Id, VehicleId = polo.Id, DrivingZoneId = centar.Id,
            StartsAtUtc = now.AddDays(3).Date.AddHours(10), EndsAtUtc = now.AddDays(3).Date.AddHours(11),
            LessonNumber = 1, Note = "Molim da vježbamo parkiranje.", Status = LessonStatus.Pending,
        };
        var confirmedLesson = new Lesson
        {
            StudentId = emir.Id, InstructorId = damir.Id, VehicleId = octavia.Id, DrivingZoneId = staroGrad.Id,
            StartsAtUtc = now.AddDays(2).Date.AddHours(14), EndsAtUtc = now.AddDays(2).Date.AddHours(15),
            LessonNumber = 1, Status = LessonStatus.Confirmed,
        };
        var completedLesson = new Lesson
        {
            StudentId = amina.Id, InstructorId = sanja.Id, VehicleId = polo.Id, DrivingZoneId = centar.Id,
            StartsAtUtc = now.AddDays(-5).Date.AddHours(9), EndsAtUtc = now.AddDays(-5).Date.AddHours(10),
            LessonNumber = 2, Status = LessonStatus.Completed, Rating = 4,
            InstructorNotes = "Dobra vožnja, treba poraditi na paralelnom parkiranju.",
            StartedAt = now.AddDays(-5).Date.AddHours(9), CompletedAt = now.AddDays(-5).Date.AddHours(10),
        };
        var cancelledLesson = new Lesson
        {
            StudentId = lejla.Id, InstructorId = sanja.Id, VehicleId = octavia.Id, DrivingZoneId = centar.Id,
            StartsAtUtc = now.AddDays(-2).Date.AddHours(16), EndsAtUtc = now.AddDays(-2).Date.AddHours(17),
            LessonNumber = 1, Status = LessonStatus.Cancelled, RejectionReason = "Vozilo nije dostupno u terminu.",
        };

        // Two lessons later today (the next full hours), so the dashboard's "today" table isn't
        // empty. Near midnight they simply fall on tomorrow.
        var nextFullHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
        var todayConfirmedLesson = new Lesson
        {
            StudentId = emir.Id, InstructorId = sanja.Id, VehicleId = polo.Id, DrivingZoneId = centar.Id,
            StartsAtUtc = nextFullHour, EndsAtUtc = nextFullHour.AddHours(1),
            LessonNumber = 2, Status = LessonStatus.Confirmed,
        };
        var todayPendingLesson = new Lesson
        {
            StudentId = lejla.Id, InstructorId = damir.Id, VehicleId = octavia.Id, DrivingZoneId = zones[2].Id,
            StartsAtUtc = nextFullHour.AddHours(2), EndsAtUtc = nextFullHour.AddHours(3),
            LessonNumber = 2, Note = "Vježba vožnje u gradu.", Status = LessonStatus.Pending,
        };

        context.Lessons.AddRange(pendingLesson, confirmedLesson, completedLesson, cancelledLesson, todayConfirmedLesson, todayPendingLesson);
        await context.SaveChangesAsync(cancellationToken);

        context.LessonStatusHistories.AddRange(
            new LessonStatusHistory { LessonId = pendingLesson.Id, FromStatus = LessonStatus.Pending, ToStatus = LessonStatus.Pending, ChangedByUserId = amina.Id, Reason = "Kreiran zahtjev za čas." },
            new LessonStatusHistory { LessonId = confirmedLesson.Id, FromStatus = LessonStatus.Pending, ToStatus = LessonStatus.Confirmed, ChangedByUserId = damir.Id, Reason = "Potvrđen termin." },
            new LessonStatusHistory { LessonId = completedLesson.Id, FromStatus = LessonStatus.Pending, ToStatus = LessonStatus.Confirmed, ChangedByUserId = sanja.Id, Reason = "Potvrđen termin." },
            new LessonStatusHistory { LessonId = completedLesson.Id, FromStatus = LessonStatus.Confirmed, ToStatus = LessonStatus.Completed, ChangedByUserId = sanja.Id, Reason = "Čas uspješno odrađen." },
            new LessonStatusHistory { LessonId = cancelledLesson.Id, FromStatus = LessonStatus.Pending, ToStatus = LessonStatus.Cancelled, ChangedByUserId = sanja.Id, Reason = "Vozilo nije dostupno u terminu." },
            new LessonStatusHistory { LessonId = todayConfirmedLesson.Id, FromStatus = LessonStatus.Pending, ToStatus = LessonStatus.Confirmed, ChangedByUserId = sanja.Id, Reason = "Potvrđen termin." },
            new LessonStatusHistory { LessonId = todayPendingLesson.Id, FromStatus = LessonStatus.Pending, ToStatus = LessonStatus.Pending, ChangedByUserId = lejla.Id, Reason = "Kreiran zahtjev za čas." });

        context.Notifications.AddRange(
            new Notification { UserId = damir.Id, Title = "Novi zahtjev za čas", Message = "Amina Hodžić je zatražila čas vožnje.", Type = NotificationType.LessonCreated },
            new Notification { UserId = emir.Id, Title = "Čas potvrđen", Message = "Vaš čas vožnje je potvrđen.", Type = NotificationType.LessonConfirmed },
            new Notification { UserId = amina.Id, Title = "Čas završen", Message = "Vaš čas vožnje je uspješno odrađen.", Type = NotificationType.LessonCompleted, IsRead = true },
            new Notification { UserId = lejla.Id, Title = "Zahtjev otkazan", Message = "Vaš zahtjev je otkazan: Vozilo nije dostupno u terminu.", Type = NotificationType.LessonCancelled });

        await context.SaveChangesAsync(cancellationToken);

        await SeedTestsAndStudyLessonsAsync(context, questionAreas, demoStudent, cancellationToken);
        await SeedExtraLessonPaymentsAsync(context, serviceTypes[3], studentUsers[4], admin, damir, sanja, polo, octavia, centar, staroGrad, cancellationToken);
    }

    /// <summary>
    /// One student (adnan.zukic) who is already past the 30-lesson package quota, with the two
    /// extra-lesson cases the payment flow needs:
    ///  - an earlier extra lesson that was paid and then cancelled+refunded - pure historical
    ///    data (the seeded payment has no real PayPal capture, so it can never be refunded
    ///    through the API again), and
    ///  - an upcoming Confirmed extra lesson more than 48h away whose charge is still UNPAID:
    ///    the one to pay with a real PayPal sandbox buyer and then cancel for a real refund.
    /// </summary>
    private static async Task SeedExtraLessonPaymentsAsync(
        EAutoskolaDbContext context, ServiceType extraLessonType, User student, User admin, User damir, User sanja,
        Vehicle polo, Vehicle octavia, DrivingZone centar, DrivingZone staroGrad, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // The 30 lessons included in the package, all already completed.
        var packageLessons = Enumerable.Range(0, PaymentConstants.PackagePracticalLessonQuota).Select(i =>
        {
            var start = now.Date.AddDays(-90 + i * 2).AddHours(9);
            return new Lesson
            {
                StudentId = student.Id, InstructorId = i % 2 == 0 ? damir.Id : sanja.Id, VehicleId = polo.Id, DrivingZoneId = centar.Id,
                StartsAtUtc = start, EndsAtUtc = start.AddHours(1), LessonNumber = i + 1,
                Status = LessonStatus.Completed, Rating = 4, StartedAt = start, CompletedAt = start.AddHours(1),
                CreatedAt = start.AddDays(-3),
            };
        }).ToList();
        context.Lessons.AddRange(packageLessons);
        await context.SaveChangesAsync(cancellationToken);

        foreach (var lesson in packageLessons)
        {
            context.LessonStatusHistories.AddRange(
                new LessonStatusHistory { LessonId = lesson.Id, FromStatus = LessonStatus.Pending, ToStatus = LessonStatus.Confirmed, ChangedByUserId = lesson.InstructorId, Reason = "Potvrđen termin.", ChangedAt = lesson.CreatedAt.AddDays(1) },
                new LessonStatusHistory { LessonId = lesson.Id, FromStatus = LessonStatus.Confirmed, ToStatus = LessonStatus.Completed, ChangedByUserId = lesson.InstructorId, Reason = "Čas uspješno odrađen.", ChangedAt = lesson.CompletedAt!.Value });
        }

        // Historical: extra lesson #31, paid, then cancelled by the admin and refunded.
        var refundedStart = now.Date.AddDays(-12).AddHours(11);
        var refundedLesson = new Lesson
        {
            StudentId = student.Id, InstructorId = sanja.Id, VehicleId = polo.Id, DrivingZoneId = centar.Id,
            StartsAtUtc = refundedStart, EndsAtUtc = refundedStart.AddHours(1), LessonNumber = PaymentConstants.PackagePracticalLessonQuota + 1,
            Status = LessonStatus.Cancelled, RejectionReason = "Otkazano od strane administracije.",
            CreatedAt = refundedStart.AddDays(-6),
        };
        context.Lessons.Add(refundedLesson);
        await context.SaveChangesAsync(cancellationToken);

        var paidAt = refundedStart.AddDays(-4);
        var refundedAt = refundedStart.AddDays(-1);
        var amountEur = CurrencyConversion.KmToEur(extraLessonType.Price);
        context.Charges.Add(new Charge
        {
            StudentId = student.Id, ServiceTypeId = extraLessonType.Id, LessonId = refundedLesson.Id,
            Amount = extraLessonType.Price, Description = $"{extraLessonType.Name} ({refundedStart:dd.MM.yyyy.})",
            Status = ChargeStatus.Cancelled, CreatedAt = paidAt.AddDays(-1),
            Installments =
            [
                new Installment
                {
                    Number = 1, Amount = extraLessonType.Price, DueDate = refundedStart, Status = InstallmentStatus.Cancelled, CreatedAt = paidAt.AddDays(-1),
                    Payments =
                    [
                        new Payment
                        {
                            Amount = extraLessonType.Price, PayPalAmount = amountEur, PayPalCurrency = "EUR", CapturedAmount = amountEur,
                            Status = PaymentStatus.Refunded, PayPalOrderId = "SEED-ORDER-0001",
                            PayPalApprovalUrl = "https://www.sandbox.paypal.com/checkoutnow?token=SEED-ORDER-0001",
                            PayPalCaptureId = "SEED-CAPTURE-0001", CreatedAt = paidAt, CompletedAt = paidAt,
                            Refunds =
                            [
                                new Refund
                                {
                                    Amount = amountEur, AmountKm = extraLessonType.Price, PayPalRefundId = "SEED-REFUND-0001",
                                    Status = RefundStatus.Completed, CreatedAt = refundedAt,
                                },
                            ],
                        },
                    ],
                },
            ],
        });
        context.LessonStatusHistories.AddRange(
            new LessonStatusHistory { LessonId = refundedLesson.Id, FromStatus = LessonStatus.Pending, ToStatus = LessonStatus.Confirmed, ChangedByUserId = sanja.Id, Reason = "Potvrđen termin.", ChangedAt = refundedStart.AddDays(-5) },
            new LessonStatusHistory { LessonId = refundedLesson.Id, FromStatus = LessonStatus.Confirmed, ToStatus = LessonStatus.Cancelled, ChangedByUserId = admin.Id, Reason = "Otkazano od strane administracije.", ChangedAt = refundedAt });

        // Upcoming: extra lesson #32, confirmed, charge unpaid.
        var upcomingStart = now.Date.AddDays(5).AddHours(10);
        var upcomingLesson = new Lesson
        {
            StudentId = student.Id, InstructorId = damir.Id, VehicleId = octavia.Id, DrivingZoneId = staroGrad.Id,
            StartsAtUtc = upcomingStart, EndsAtUtc = upcomingStart.AddHours(1), LessonNumber = PaymentConstants.PackagePracticalLessonQuota + 2,
            Status = LessonStatus.Confirmed,
        };
        context.Lessons.Add(upcomingLesson);
        await context.SaveChangesAsync(cancellationToken);

        context.Charges.Add(new Charge
        {
            StudentId = student.Id, ServiceTypeId = extraLessonType.Id, LessonId = upcomingLesson.Id,
            Amount = extraLessonType.Price, Description = $"{extraLessonType.Name} ({upcomingStart:dd.MM.yyyy.})",
            Installments = [new Installment { Number = 1, Amount = extraLessonType.Price, DueDate = upcomingStart }],
        });
        context.LessonStatusHistories.Add(
            new LessonStatusHistory { LessonId = upcomingLesson.Id, FromStatus = LessonStatus.Pending, ToStatus = LessonStatus.Confirmed, ChangedByUserId = damir.Id, Reason = "Potvrđen termin." });
        context.Notifications.Add(new Notification
        {
            UserId = student.Id, Title = "Čas potvrđen", Type = NotificationType.LessonConfirmed,
            Message = $"Vaš zahtjev za čas vožnje je potvrđen. Ovaj čas nije obuhvaćen paketom i potrebno ga je platiti ({extraLessonType.Price:F2} KM).",
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Tests/questions/study lessons, plus one real TestAttempt with intentional
    /// errors concentrated in "Saobraćajni znakovi" for the demo student - gives the
    /// Recommender module genuine signal to work with instead of an empty cold-start.</summary>
    private static async Task SeedTestsAndStudyLessonsAsync(EAutoskolaDbContext context, QuestionArea[] questionAreas, User demoStudent, CancellationToken cancellationToken)
    {
        var signsArea = questionAreas[0]; // Saobraćajni znakovi
        var rulesArea = questionAreas[1]; // Pravila vožnje
        var situationsArea = questionAreas[2]; // Situacije u saobraćaju
        var firstAidArea = questionAreas[3]; // Prva pomoć
        var maintenanceArea = questionAreas[4]; // Održavanje vozila

        // --- Study lessons (docs/prijava.md §4.2.9) ---
        var studyLessons = new[]
        {
            new StudyLesson { Title = "Saobraćajni znakovi", QuestionAreaId = signsArea.Id, Tags = "saobracajni znakovi, zabrana, obaveza, opasnost", ValidForYear = 2026, ReadingMinutes = 8, ChapterLabel = "Poglavlje 3.1" },
            new StudyLesson { Title = "Pravila vožnje", QuestionAreaId = rulesArea.Id, Tags = "pravila voznje, prvenstvo prolaza, brzina", ValidForYear = 2026, ReadingMinutes = 10, ChapterLabel = "Poglavlje 2.1" },
            new StudyLesson { Title = "Situacije u saobraćaju", QuestionAreaId = situationsArea.Id, Tags = "raskrsnice, situacije, pjesaci", ValidForYear = 2026, ReadingMinutes = 9, ChapterLabel = "Poglavlje 4.1" },
            new StudyLesson { Title = "Prva pomoć", QuestionAreaId = firstAidArea.Id, Tags = "prva pomoc, povrede, hitna pomoc", ValidForYear = 2026, ReadingMinutes = 6, ChapterLabel = "Poglavlje 5.1" },
            new StudyLesson { Title = "Održavanje vozila", QuestionAreaId = maintenanceArea.Id, Tags = "odrzavanje, motor, gume, tekucine", ValidForYear = 2026, ReadingMinutes = 7, ChapterLabel = "Poglavlje 6.1" },
        };
        context.StudyLessons.AddRange(studyLessons);
        await context.SaveChangesAsync(cancellationToken);

        foreach (var lesson in studyLessons)
        {
            context.StudyLessonSections.AddRange(
                new StudyLessonSection { StudyLessonId = lesson.Id, Title = "Uvod", Content = $"Uvod u oblast: {lesson.Title}.", SortOrder = 1 },
                new StudyLessonSection { StudyLessonId = lesson.Id, Title = "Osnovna pravila", Content = $"Detaljan opis osnovnih pravila za {lesson.Title.ToLowerInvariant()}.", SortOrder = 2 },
                new StudyLessonSection { StudyLessonId = lesson.Id, Title = "Primjeri", Content = "Praktični primjeri i česte greške kandidata.", SortOrder = 3 });
        }
        await context.SaveChangesAsync(cancellationToken);

        // --- Test 1: Saobraćajni znakovi (seeded with a real, scored attempt below) ---
        var signsTest = new Test { Title = "Test 1 - Saobraćajni znakovi", QuestionAreaId = signsArea.Id, PassThresholdPercent = 70 };
        var rulesTest = new Test { Title = "Test 2 - Pravila vožnje", QuestionAreaId = rulesArea.Id, PassThresholdPercent = 70 };
        context.Tests.AddRange(signsTest, rulesTest);
        await context.SaveChangesAsync(cancellationToken);

        var signsQuestions = new[]
        {
            BuildQuestion(signsTest.Id, signsArea.Id, "Koji znak označava obavezno skretanje udesno?", ["Zabrana skretanja", "Obavezno skretanje udesno", "Jednosmjerni saobraćaj", "Opasna krivina"], 1),
            BuildQuestion(signsTest.Id, signsArea.Id, "Šta označava trougaoni znak sa crvenim okvirom?", ["Obavezu", "Zabranu", "Opasnost", "Obavještenje"], 2),
            BuildQuestion(signsTest.Id, signsArea.Id, "Okrugli znak sa plavom pozadinom označava:", ["Zabranu", "Opasnost", "Obavezu", "Prvenstvo prolaza"], 2),
            BuildQuestion(signsTest.Id, signsArea.Id, "Znak 'Stop' zahtijeva:", ["Usporavanje", "Potpuno zaustavljanje", "Promjenu trake", "Ubrzanje"], 1),
            BuildQuestion(signsTest.Id, signsArea.Id, "Dopunska tabla ispod znaka obično označava:", ["Dodatno pojašnjenje znaka", "Novi znak", "Prestanak važenja prethodnog znaka", "Ništa posebno"], 0),
        };
        var rulesQuestions = new[]
        {
            BuildQuestion(rulesTest.Id, rulesArea.Id, "Ko ima prvenstvo prolaza na raskrsnici bez signalizacije?", ["Vozilo sa lijeve strane", "Vozilo sa desne strane", "Brže vozilo", "Veće vozilo"], 1),
            BuildQuestion(rulesTest.Id, rulesArea.Id, "Maksimalna brzina u naselju, osim ako nije drugačije označeno, je:", ["50 km/h", "60 km/h", "70 km/h", "80 km/h"], 0),
            BuildQuestion(rulesTest.Id, rulesArea.Id, "Preticanje je zabranjeno:", ["Na pravom putu", "Na raskrsnici", "Na autoputu", "U naselju"], 1),
        };

        context.Questions.AddRange(signsQuestions.Select(q => q.Question).Concat(rulesQuestions.Select(q => q.Question)));
        await context.SaveChangesAsync(cancellationToken);

        foreach (var (question, options) in signsQuestions.Concat(rulesQuestions))
        {
            foreach (var option in options)
            {
                option.QuestionId = question.Id;
            }
            context.AnswerOptions.AddRange(options);
        }
        await context.SaveChangesAsync(cancellationToken);

        // --- A real, scored TestAttempt for the demo student: 2/5 correct on the Signs test,
        // with every error concentrated in the Signs area - clear recommender signal. ---
        var attempt = new TestAttempt
        {
            StudentId = demoStudent.Id, TestId = signsTest.Id,
            StartedAt = DateTime.UtcNow.AddMinutes(-20), SubmittedAt = DateTime.UtcNow.AddMinutes(-15),
            CorrectCount = 2, IncorrectCount = 3, Percent = 40, Passed = false,
        };
        context.TestAttempts.Add(attempt);
        await context.SaveChangesAsync(cancellationToken);

        // Questions 1 and 4 (index 0, 3) answered correctly; 2, 3, 5 answered wrong.
        var correctIndexes = new HashSet<int> { 0, 3 };
        for (var i = 0; i < signsQuestions.Length; i++)
        {
            var (question, options) = signsQuestions[i];
            var correctOption = options.First(o => o.IsCorrect);
            var selectedOption = correctIndexes.Contains(i) ? correctOption : options.First(o => !o.IsCorrect);

            context.AttemptAnswers.Add(new AttemptAnswer
            {
                TestAttemptId = attempt.Id, QuestionId = question.Id,
                SelectedAnswerOptionId = selectedOption.Id, IsCorrect = selectedOption.IsCorrect,
            });
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static (Question Question, AnswerOption[] Options) BuildQuestion(int testId, int questionAreaId, string text, string[] optionTexts, int correctIndex)
    {
        var question = new Question { Text = text, QuestionAreaId = questionAreaId, TestId = testId };
        var options = optionTexts.Select((t, i) => new AnswerOption { Text = t, IsCorrect = i == correctIndex }).ToArray();
        return (question, options);
    }
}
