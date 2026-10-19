using eAutoskola.Application.ReferenceData.RequestStatuses;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.ReferenceData;

/// <summary>The four statuses of the lesson flow are looked up by the state machine, so the admin
/// may rename them but not re-code or delete them; any other status is plain reference data.</summary>
public class RequestStatusServiceTests
{
    static RequestStatusServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(RequestStatusService).Assembly);

    private static (RequestStatusService Service, EAutoskolaDbContext Context) Create()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.RequestStatuses.AddRange(
            new RequestStatus { Id = 1, Code = nameof(LessonStatus.Pending), Name = "Na čekanju" },
            new RequestStatus { Id = 2, Code = "Archived", Name = "Arhiviran" });
        context.SaveChanges();
        return (new RequestStatusService(context), context);
    }

    [Fact]
    public async Task AStatusOfTheLessonFlow_CanBeRenamed()
    {
        var (service, context) = Create();

        await service.UpdateAsync(1, new RequestStatusRequest { Code = "Pending", Name = "Čeka odgovor", Description = "Novi opis" });

        Assert.Equal("Čeka odgovor", (await context.RequestStatuses.AsNoTracking().SingleAsync(s => s.Id == 1)).Name);
    }

    [Fact]
    public async Task AStatusOfTheLessonFlow_CannotBeReCoded()
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<BusinessException>(() => service.UpdateAsync(1, new RequestStatusRequest { Code = "Waiting", Name = "Na čekanju" }));
    }

    [Fact]
    public async Task AStatusOfTheLessonFlow_CannotBeDeleted()
    {
        var (service, context) = Create();

        await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(1));

        Assert.Equal(2, await context.RequestStatuses.CountAsync());
    }

    [Fact]
    public async Task AnyOtherStatus_IsOrdinaryReferenceData()
    {
        var (service, context) = Create();

        await service.UpdateAsync(2, new RequestStatusRequest { Code = "Stored", Name = "Pohranjen" });
        await service.DeleteAsync(2);

        Assert.Equal([1], await context.RequestStatuses.Select(s => s.Id).ToListAsync());
    }
}
