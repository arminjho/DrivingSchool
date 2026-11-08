using eAutoskola.Application.ReferenceData.DrivingZones;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.ReferenceData;

/// <summary>Dorada #9/#11: DrivingZoneService had no validation and no delete-reason message.</summary>
public class DrivingZoneServiceTests
{
    static DrivingZoneServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(DrivingZoneService).Assembly);

    private static (DrivingZoneService Service, EAutoskolaDbContext Context) Create()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.DrivingZones.Add(new DrivingZone { Id = 1, Name = "Centar" });
        context.SaveChanges();
        return (new DrivingZoneService(context), context);
    }

    [Fact]
    public async Task Insert_WithABlankName_IsRejected()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new DrivingZoneRequest { Name = "" }));
    }

    [Fact]
    public async Task Insert_WithANameOver100Characters_IsRejected()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new DrivingZoneRequest { Name = new string('x', 101) }));
    }

    [Fact]
    public async Task Delete_AZoneUsedByALesson_IsRejectedWithTheLessonCount()
    {
        var (service, context) = Create();
        context.Lessons.Add(new Lesson
        {
            Id = 1, StudentId = 1, InstructorId = 2, DrivingZoneId = 1,
            StartsAtUtc = DateTime.UtcNow.AddDays(1), EndsAtUtc = DateTime.UtcNow.AddDays(1).AddHours(1), Status = LessonStatus.Pending,
        });
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(1));
        Assert.Contains("1", ex.Message);
    }

    [Fact]
    public async Task Delete_AnUnusedZone_Succeeds()
    {
        var (service, context) = Create();

        await service.DeleteAsync(1);

        Assert.Equal(0, await context.DrivingZones.CountAsync());
    }
}
