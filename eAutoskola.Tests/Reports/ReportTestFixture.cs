using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.Reports;

/// <summary>An in-memory database with small builders for the people, vehicles and lessons the
/// dashboard and report tests need.</summary>
public class ReportTestFixture
{
    public EAutoskolaDbContext Context { get; } = new(
        new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private int _nextId = 1;

    public DrivingZone Zone { get; }
    public VehicleCategory CategoryB { get; }
    public VehicleCategory CategoryC { get; }
    public VehicleServiceType ServiceTypeRegular { get; }
    public VehicleServiceType ServiceTypeOil { get; }

    public ReportTestFixture()
    {
        Zone = new DrivingZone { Id = 1, Name = "Centar" };
        CategoryB = new VehicleCategory { Id = 1, Code = "B", Name = "Osobna vozila" };
        CategoryC = new VehicleCategory { Id = 2, Code = "C", Name = "Teretna vozila" };
        ServiceTypeRegular = new VehicleServiceType { Id = 1, Name = "Redovni servis" };
        ServiceTypeOil = new VehicleServiceType { Id = 2, Name = "Zamjena ulja" };
        Context.AddRange(Zone, CategoryB, CategoryC, ServiceTypeRegular, ServiceTypeOil);
        Context.SaveChanges();
    }

    public User AddUser(string role, string firstName, string lastName, bool isActive = true, DateTime? createdAtUtc = null, bool isDeleted = false)
    {
        var id = _nextId++;
        var user = new User
        {
            Id = id, Email = $"user{id}@test.ba", PasswordHash = "x", Role = role, FirstName = firstName, LastName = lastName,
            PhoneNumber = "+387 61 000 000", IsActive = isActive, CreatedAt = createdAtUtc ?? DateTime.UtcNow, IsDeleted = isDeleted,
        };
        Context.Users.Add(user);
        Context.SaveChanges();
        return user;
    }

    public User AddInstructor(string firstName = "Damir", string lastName = "Kovačević", bool isActive = true, bool isDeleted = false)
        => AddUser(Role.Instructor, firstName, lastName, isActive, isDeleted: isDeleted);

    public User AddStudent(string firstName = "Amina", string lastName = "Hodžić", bool isActive = true, DateTime? createdAtUtc = null)
        => AddUser(Role.Student, firstName, lastName, isActive, createdAtUtc);

    public Lesson AddLesson(User student, User instructor, DateTime startsAtUtc, LessonStatus status, DateTime? createdAtUtc = null)
    {
        var lesson = new Lesson
        {
            StudentId = student.Id, InstructorId = instructor.Id, DrivingZoneId = Zone.Id,
            StartsAtUtc = startsAtUtc, EndsAtUtc = startsAtUtc.AddHours(1), Status = status,
            CreatedAt = createdAtUtc ?? startsAtUtc.AddDays(-2),
        };
        Context.Lessons.Add(lesson);
        Context.SaveChanges();
        return lesson;
    }

    public Vehicle AddVehicle(string registration, VehicleCategory category, VehicleStatus status = VehicleStatus.Active, bool isDeleted = false, int year = 2021)
    {
        var vehicle = new Vehicle
        {
            Id = _nextId++, RegistrationNumber = registration, Make = "VW", Model = "Polo", VehicleCategoryId = category.Id,
            ManufactureYear = year, Status = status, IsDeleted = isDeleted,
        };
        Context.Vehicles.Add(vehicle);
        Context.SaveChanges();
        return vehicle;
    }

    public VehicleService AddService(Vehicle vehicle, DateTime serviceDateUtc, decimal cost, VehicleServiceStatus status = VehicleServiceStatus.Completed, VehicleServiceType? type = null, DateTime? createdAtUtc = null)
    {
        var service = new VehicleService
        {
            Id = _nextId++, VehicleId = vehicle.Id, ServiceDate = serviceDateUtc, VehicleServiceTypeId = (type ?? ServiceTypeRegular).Id,
            Description = "Opis servisa", Cost = cost, Status = status, CreatedAt = createdAtUtc ?? serviceDateUtc,
        };
        Context.VehicleServices.Add(service);
        Context.SaveChanges();
        return service;
    }

    public static DateTime Utc(int year, int month, int day, int hour = 10) => new(year, month, day, hour, 0, 0, DateTimeKind.Utc);
}
