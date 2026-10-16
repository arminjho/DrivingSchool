using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Instructors;

public class InstructorQueryService : BaseQueryService<InstructorDto, InstructorSearchObject, User>
{
    private readonly EAutoskolaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public InstructorQueryService(EAutoskolaDbContext context, ICurrentUserService currentUser) : base(context)
    {
        _context = context;
        _currentUser = currentUser;
    }

    protected override IQueryable<User> ApplyFilter(IQueryable<User> query, InstructorSearchObject search)
    {
        query = query.Include(u => u.InstructorProfile!).ThenInclude(p => p.VehicleCategory)
            .Where(u => u.Role == Role.Instructor && u.IsActive);

        if (search.VehicleCategoryId.HasValue)
        {
            query = query.Where(u => u.InstructorProfile!.VehicleCategoryId == search.VehicleCategoryId.Value);
        }
        return query;
    }

    /// <summary>"Moji učenici" - distinct students who have at least one lesson with the
    /// current instructor, with each student's lesson count against this instructor.</summary>
    public async Task<PagedResult<StudentSummaryDto>> GetMyStudentsAsync(StudentSearchObject search, CancellationToken cancellationToken)
    {
        var instructorId = _currentUser.UserId;

        var studentIdsQuery = _context.Lessons
            .Where(l => l.InstructorId == instructorId)
            .Select(l => l.StudentId)
            .Distinct();

        var query = _context.Users
            .Include(u => u.StudentProfile!).ThenInclude(p => p.VehicleCategory)
            .Where(u => studentIdsQuery.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(search.Search))
        {
            query = query.Where(u => u.FirstName.Contains(search.Search) || u.LastName.Contains(search.Search) || u.Email.Contains(search.Search));
        }

        var count = await query.CountAsync(cancellationToken);
        var students = await query
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync(cancellationToken);

        var lessonCounts = await _context.Lessons
            .Where(l => l.InstructorId == instructorId && studentIdsQuery.Contains(l.StudentId))
            .GroupBy(l => l.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StudentId, x => x.Count, cancellationToken);

        var result = students.Select(s =>
        {
            var dto = s.Adapt<StudentSummaryDto>();
            dto.TotalLessons = lessonCounts.GetValueOrDefault(s.Id);
            return dto;
        }).ToList();

        return new PagedResult<StudentSummaryDto> { ResultList = result, Count = count };
    }
}
