using eAutoskola.Application.Tests;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.Theory;

/// <summary>Dorada #10: TestRequest.PassThresholdPercent had no range check - a threshold of -1
/// made every result pass and 101 made every result fail.</summary>
public class TestCrudServiceTests
{
    static TestCrudServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(TestCrudService).Assembly);

    private static (TestCrudService Service, EAutoskolaDbContext Context) Create()
    {
        var context = new EAutoskolaDbContext(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.QuestionAreas.Add(new QuestionArea { Id = 1, Name = "Saobraćajni znakovi" });
        context.SaveChanges();
        return (new TestCrudService(context), context);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Insert_WithAPassThresholdOutsideZeroToOneHundred_IsRejected(int threshold)
    {
        var (service, _) = Create();

        await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(
            new TestRequest { Title = "Test 1", QuestionAreaId = 1, PassThresholdPercent = threshold }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70)]
    [InlineData(100)]
    public async Task Insert_WithAPassThresholdWithinRange_Succeeds(int threshold)
    {
        var (service, context) = Create();

        await service.InsertAsync(new TestRequest { Title = "Test 1", QuestionAreaId = 1, PassThresholdPercent = threshold });

        Assert.Equal(1, await context.Tests.CountAsync());
    }
}
