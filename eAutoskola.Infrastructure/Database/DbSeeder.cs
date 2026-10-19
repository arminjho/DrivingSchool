using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Payments;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database.Seed;
using eAutoskola.Infrastructure.Payments;
using eAutoskola.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Infrastructure.Database;

/// <summary>
/// Idempotent runtime seeder (not EF HasData, so PasswordHasher - the same one AuthService and
/// UserService use - is called directly; this is how seed and runtime password hashes stay in
/// one format, per upute.md's warning against mixing them). Creates a database that can actually
/// be tested: 3 countries / 12 cities, 5 licence categories, 2 admins, 6 instructors,
/// 24 students, 16 vehicles with maintenance history, ~200 lessons in every status spread over the
/// year, package charges with installments and paid history, 6 theory tests with 60 questions,
/// ~40 test attempts, 12 study lessons and the notifications of all that. Profile, vehicle and
/// question images are included. Values follow docs/prijava.md's mockups where they show rows.
/// Everything is derived from the seeding day with simple arithmetic - no randomness - so two
/// fresh databases look alike. The steps live in the Seed folder.
/// </summary>
public static class DbSeeder
{
    private const string DemoPassword = "test";

    public static async Task SeedAsync(EAutoskolaDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Countries.AnyAsync(cancellationToken))
        {
            return; // already seeded
        }

        var now = DateTime.UtcNow;

        // The seed is many saves (each step needs the ids the previous one generated), so on a real
        // database they run in ONE explicit transaction (upute.md §3.4): a failure part-way leaves
        // nothing behind, and the next start seeds from scratch instead of finding a half-filled
        // database that the "already seeded" check above would accept.
        await using var transaction = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync(cancellationToken) : null;

        var reference = await SeedReferenceDataAsync(context, cancellationToken);
        var people = await SeedPeopleAsync(context, reference, now, cancellationToken);
        var fleet = await SeedFleetAsync(context, reference, now, cancellationToken);

        var lessons = await SeedLessonsStep.RunAsync(context, reference, people, fleet, now, cancellationToken);
        var payments = await SeedFinanceStep.RunAsync(context, reference, people, lessons, now, cancellationToken);
        await SeedTheoryStep.RunAsync(context, reference, people, now, cancellationToken);
        await SeedNotificationsStep.RunAsync(context, reference, people, lessons, payments, now, cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    private static async Task<SeedReference> SeedReferenceDataAsync(EAutoskolaDbContext context, CancellationToken cancellationToken)
    {
        var countries = SeedFleetAndPeopleData.Countries.Select(c => new Country { Code = c.Code, Name = c.Name }).ToArray();
        context.Countries.AddRange(countries);
        await context.SaveChangesAsync(cancellationToken);

        var cities = SeedFleetAndPeopleData.Cities
            .Select(c => new City { Name = c.Name, PostalCode = c.PostalCode, CountryId = countries.Single(country => country.Code == c.CountryCode).Id })
            .ToArray();
        context.Cities.AddRange(cities);

        var categories = SeedFleetAndPeopleData.VehicleCategories
            .Select(c => new VehicleCategory { Code = c.Code, Name = c.Name, Description = c.Description })
            .ToArray();
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

        var vehicleServiceTypes = SeedFleetAndPeopleData.VehicleServiceTypes.Select(name => new VehicleServiceType { Name = name }).ToArray();
        context.VehicleServiceTypes.AddRange(vehicleServiceTypes);

        var zones = SeedFleetAndPeopleData.DrivingZones.Select(name => new DrivingZone { Name = name }).ToArray();
        context.DrivingZones.AddRange(zones);

        var questionAreas = SeedFleetAndPeopleData.QuestionAreas.Select(name => new QuestionArea { Name = name }).ToArray();
        context.QuestionAreas.AddRange(questionAreas);

        await context.SaveChangesAsync(cancellationToken);

        return new SeedReference(cities, categories.ToDictionary(c => c.Code), requestStatuses.ToDictionary(s => s.Code), serviceTypes, vehicleServiceTypes, zones, questionAreas);
    }

    private static async Task<SeedPeople> SeedPeopleAsync(EAutoskolaDbContext context, SeedReference reference, DateTime now, CancellationToken cancellationToken)
    {
        var passwordHash = PasswordHasher.Hash(DemoPassword);
        var ordinal = 0;

        User Build(SeedPerson person, string role, DateTime createdAt) => new()
        {
            Email = SeedFleetAndPeopleData.LoginFor(person),
            PasswordHash = passwordHash,
            Role = role,
            FirstName = person.FirstName,
            LastName = person.LastName,
            PhoneNumber = SeedFleetAndPeopleData.PhoneFor(ordinal++),
            CityId = reference.Cities[person.CityIndex].Id,
            ProfileImagePath = person.Photo ? SeedImages.Copy(SeedFleetAndPeopleData.PhotoFileFor(person)) : null,
            IsActive = person.Active,
            CreatedAt = createdAt,
        };

        var admins = SeedFleetAndPeopleData.Admins
            .Select((person, i) => Build(person, Role.Admin, now.AddDays(-340 + i)))
            .ToArray();

        var instructors = SeedFleetAndPeopleData.Instructors
            .Select((person, i) =>
            {
                var user = Build(person, Role.Instructor, now.AddDays(-330 + i * 3));
                user.InstructorProfile = new InstructorProfile { VehicleCategoryId = reference.Categories[person.Category].Id };
                return user;
            })
            .ToArray();

        var establishedCount = SeedFleetAndPeopleData.Students.Length - SeedFleetAndPeopleData.NewStudentCount;
        var students = SeedFleetAndPeopleData.Students
            .Select((person, i) =>
            {
                // The last few students registered this month (dashboard "+N ovaj mjesec").
                var createdAt = i < establishedCount
                    ? now.AddDays(-320 + i * 14)
                    : now.Date.AddHours(6).AddDays(-(i - establishedCount));
                var user = Build(person, Role.Student, createdAt);
                user.StudentProfile = new StudentProfile { VehicleCategoryId = reference.Categories[person.Category].Id, RegisteredAt = createdAt };
                return user;
            })
            .ToArray();

        context.Users.AddRange(admins);
        context.Users.AddRange(instructors);
        context.Users.AddRange(students);

        // Same method self-registration and the admin "Novi korisnik" use.
        foreach (var student in students)
        {
            await PackageChargeProvisioner.AddPackageChargesAsync(context, student, cancellationToken);
        }

        // The package belongs to the day the student registered, not to the seeding day.
        foreach (var charge in context.Charges.Local)
        {
            charge.CreatedAt = charge.Student!.CreatedAt;
            foreach (var installment in charge.Installments)
            {
                installment.CreatedAt = charge.Student.CreatedAt;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return new SeedPeople(admins, instructors, students);
    }

    private static async Task<SeedFleet> SeedFleetAsync(EAutoskolaDbContext context, SeedReference reference, DateTime now, CancellationToken cancellationToken)
    {
        var vehicles = SeedFleetAndPeopleData.Vehicles
            .Select(v => new Vehicle
            {
                RegistrationNumber = v.RegistrationNumber,
                Make = v.Make,
                Model = v.Model,
                VehicleCategoryId = reference.Categories[v.Category].Id,
                ManufactureYear = v.Year,
                Status = v.Status,
                ImagePath = v.Photo is null ? null : SeedImages.Copy(v.Photo),
            })
            .ToArray();
        context.Vehicles.AddRange(vehicles);

        context.VehicleServices.AddRange(SeedFleetAndPeopleData.Maintenance.Select(m => new VehicleService
        {
            Vehicle = vehicles[m.VehicleIndex],
            VehicleServiceTypeId = reference.VehicleServiceTypes[m.TypeIndex].Id,
            ServiceDate = now.Date.AddDays(m.DaysFromToday),
            Description = m.Description,
            Cost = m.Cost,
            Status = m.Status,
            CreatedAt = m.Status == VehicleServiceStatus.Scheduled ? now.AddDays(-3) : now.Date.AddDays(m.DaysFromToday).AddHours(8),
        }));

        await context.SaveChangesAsync(cancellationToken);
        return new SeedFleet(vehicles);
    }
}
