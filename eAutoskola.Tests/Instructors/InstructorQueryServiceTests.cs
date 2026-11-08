using eAutoskola.Application.Instructors;
using eAutoskola.Tests.Payments;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.Instructors;

/// <summary>GET /api/instructors/{id} has no role restriction of its own - it must filter by
/// itself (GetByIdQuery), same as the list endpoint (ApplyFilter), or it leaks any user's
/// name/photo by id, student or admin included.</summary>
public class InstructorQueryServiceTests
{
    static InstructorQueryServiceTests() => TypeAdapterConfig.GlobalSettings.Scan(typeof(InstructorQueryService).Assembly);

    private static InstructorQueryService Create(PaymentTestFixture f) => new(f.Context, f.CurrentUser.Object);

    [Fact]
    public async Task GetById_ReturnsTheInstructor_WhenTheIdIsAnActiveInstructor()
    {
        var f = new PaymentTestFixture();
        var service = Create(f);

        var result = await service.GetByIdAsync(PaymentTestFixture.InstructorId, default);

        Assert.NotNull(result);
        Assert.Equal(PaymentTestFixture.InstructorId, result!.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNull_WhenTheIdIsAStudent()
    {
        var f = new PaymentTestFixture();
        var service = Create(f);

        var result = await service.GetByIdAsync(PaymentTestFixture.StudentId, default);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_ReturnsNull_WhenTheInstructorIsDeactivated()
    {
        var f = new PaymentTestFixture();
        var instructor = await f.Context.Users.SingleAsync(u => u.Id == PaymentTestFixture.InstructorId);
        instructor.IsActive = false;
        await f.Context.SaveChangesAsync();
        var service = Create(f);

        var result = await service.GetByIdAsync(PaymentTestFixture.InstructorId, default);

        Assert.Null(result);
    }
}
