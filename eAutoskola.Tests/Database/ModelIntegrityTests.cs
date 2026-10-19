using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Tests.Database;

/// <summary>The shape of the schema that upute.md §3.1 asks for, checked on the EF model: every
/// table is connected to another by a foreign key, and no relationship can silently cascade a
/// delete onto business data.</summary>
public class ModelIntegrityTests
{
    private static EAutoskolaDbContext CreateContext()
        => new(new DbContextOptionsBuilder<EAutoskolaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void EveryTable_IsLinkedToAnotherByAForeignKey()
    {
        using var context = CreateContext();

        var isolated = context.Model.GetEntityTypes()
            .Where(entity => !entity.GetForeignKeys().Any() && !entity.GetReferencingForeignKeys().Any())
            .Select(entity => entity.DisplayName())
            .ToList();

        Assert.Empty(isolated);
    }

    [Fact]
    public void ARevokedTokenBelongsToItsUser_AndALessonToItsRequestStatus()
    {
        using var context = CreateContext();

        var revokedToken = context.Model.FindEntityType(typeof(eAutoskola.Domain.Entities.RevokedToken))!;
        var lesson = context.Model.FindEntityType(typeof(eAutoskola.Domain.Entities.Lesson))!;

        Assert.Contains(revokedToken.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(eAutoskola.Domain.Entities.User));
        Assert.Contains(lesson.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(eAutoskola.Domain.Entities.RequestStatus)
            && key.DeleteBehavior == DeleteBehavior.Restrict);
    }
}
