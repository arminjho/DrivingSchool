using eAutoskola.Application.Common;
using eAutoskola.Application.Payments;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Lessons;

public class LessonQueryService
{
    /// <summary>Instructor working hours - simple constants, not per-instructor configurable
    /// (kept deliberately simple per upute.md's anti-overengineering guidance).</summary>
    private static readonly TimeSpan WorkDayStart = TimeSpan.FromHours(8);
    private static readonly TimeSpan WorkDayEnd = TimeSpan.FromHours(18);
    private static readonly TimeSpan SlotDuration = TimeSpan.FromHours(1);

    private readonly EAutoskolaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public LessonQueryService(EAutoskolaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<DateTime>> GetAvailableSlotsAsync(int instructorId, DateOnly date, CancellationToken cancellationToken)
    {
        var dayStartUtc = date.ToDateTime(TimeOnly.FromTimeSpan(WorkDayStart), DateTimeKind.Utc);
        var dayEndUtc = date.ToDateTime(TimeOnly.FromTimeSpan(WorkDayEnd), DateTimeKind.Utc);

        var bookedSlots = await _context.Lessons
            .Where(l => l.InstructorId == instructorId
                && (l.Status == LessonStatus.Pending || l.Status == LessonStatus.Confirmed)
                && l.StartsAtUtc >= dayStartUtc && l.StartsAtUtc < dayEndUtc)
            .Select(l => l.StartsAtUtc)
            .ToListAsync(cancellationToken);

        var slots = new List<DateTime>();
        for (var slotStart = dayStartUtc; slotStart + SlotDuration <= dayEndUtc; slotStart += SlotDuration)
        {
            if (!bookedSlots.Contains(slotStart) && slotStart > DateTime.UtcNow)
            {
                slots.Add(slotStart);
            }
        }

        return slots;
    }

    public async Task<LessonDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var lesson = await LessonDetailQuery().FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Lesson), id);

        EnsureOwnerOrAdmin(lesson);
        return (await ToDtosAsync([lesson], cancellationToken))[0];
    }

    public async Task<PagedResult<LessonDto>> GetMyLessonsAsync(MyLessonsSearchObject search, CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId;
        var query = LessonDetailQuery().Where(l => l.StudentId == studentId);

        query = search.Tab.ToLowerInvariant() switch
        {
            "completed" => query.Where(l => l.Status == LessonStatus.Completed),
            "cancelled" => query.Where(l => l.Status == LessonStatus.Cancelled),
            _ => query.Where(l => l.Status == LessonStatus.Pending || l.Status == LessonStatus.Confirmed),
        };

        return await ToPagedResultAsync(query, search, cancellationToken);
    }

    public async Task<PagedResult<LessonDto>> GetLessonRequestsAsync(LessonRequestSearchObject search, CancellationToken cancellationToken)
    {
        var instructorId = _currentUser.UserId;
        var query = LessonDetailQuery().Where(l => l.InstructorId == instructorId);

        query = search.Status.ToLowerInvariant() switch
        {
            "pending" => query.Where(l => l.Status == LessonStatus.Pending),
            "confirmed" => query.Where(l => l.Status == LessonStatus.Confirmed),
            "cancelled" => query.Where(l => l.Status == LessonStatus.Cancelled),
            _ => query,
        };

        return await ToPagedResultAsync(query, search, cancellationToken);
    }

    public async Task<LessonRequestCountsDto> GetLessonRequestCountsAsync(CancellationToken cancellationToken)
    {
        var instructorId = _currentUser.UserId;
        var statuses = await _context.Lessons
            .Where(l => l.InstructorId == instructorId)
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new LessonRequestCountsDto
        {
            All = statuses.Sum(s => s.Count),
            Pending = statuses.FirstOrDefault(s => s.Status == LessonStatus.Pending)?.Count ?? 0,
            Confirmed = statuses.FirstOrDefault(s => s.Status == LessonStatus.Confirmed)?.Count ?? 0,
            Cancelled = statuses.FirstOrDefault(s => s.Status == LessonStatus.Cancelled)?.Count ?? 0,
        };
    }

    public async Task<List<LessonDto>> GetScheduleAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var instructorId = _currentUser.UserId;
        var dayStartUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEndUtc = dayStartUtc.AddDays(1);

        var lessons = await LessonDetailQuery()
            .Where(l => l.InstructorId == instructorId && l.StartsAtUtc >= dayStartUtc && l.StartsAtUtc < dayEndUtc)
            .OrderBy(l => l.StartsAtUtc)
            .ToListAsync(cancellationToken);

        return await ToDtosAsync(lessons, cancellationToken);
    }

    public async Task<InstructorStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        var instructorId = _currentUser.UserId;
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var completedLessons = await _context.Lessons
            .Where(l => l.InstructorId == instructorId && l.Status == LessonStatus.Completed)
            .Select(l => new { l.CompletedAt, l.Rating })
            .ToListAsync(cancellationToken);

        return new InstructorStatisticsDto
        {
            CompletedTotal = completedLessons.Count,
            CompletedThisMonth = completedLessons.Count(l => l.CompletedAt >= monthStart),
            AverageRating = completedLessons.Count > 0 ? completedLessons.Average(l => l.Rating ?? 0) : 0,
        };
    }

    public async Task<InstructorHomeDto> GetHomeAsync(CancellationToken cancellationToken)
    {
        var instructorId = _currentUser.UserId;
        var todayStartUtc = DateTime.UtcNow.Date;
        var todayEndUtc = todayStartUtc.AddDays(1);

        var todayLessons = await LessonDetailQuery()
            .Where(l => l.InstructorId == instructorId && l.StartsAtUtc >= todayStartUtc && l.StartsAtUtc < todayEndUtc)
            .OrderBy(l => l.StartsAtUtc)
            .ToListAsync(cancellationToken);

        var newRequestCount = await _context.Lessons
            .CountAsync(l => l.InstructorId == instructorId && l.Status == LessonStatus.Pending, cancellationToken);

        return new InstructorHomeDto
        {
            TodayLessonCount = todayLessons.Count,
            NewRequestCount = newRequestCount,
            TodayLessons = await ToDtosAsync(todayLessons, cancellationToken),
        };
    }

    private void EnsureOwnerOrAdmin(Lesson lesson)
    {
        if (_currentUser.IsInRole(Role.Admin))
        {
            return;
        }
        if (lesson.StudentId != _currentUser.UserId && lesson.InstructorId != _currentUser.UserId)
        {
            throw new ForbiddenException(Messages.NotOwner);
        }
    }

    private IQueryable<Lesson> LessonDetailQuery()
        => _context.Lessons
            .Include(l => l.Student)
            .Include(l => l.Instructor)
            .Include(l => l.Vehicle)
            .Include(l => l.DrivingZone);

    private async Task<List<LessonDto>> ToDtosAsync(IReadOnlyCollection<Lesson> lessons, CancellationToken cancellationToken)
    {
        var dtos = lessons.Adapt<List<LessonDto>>();
        await LessonPaymentStatus.ApplyAsync(_context, dtos, cancellationToken);
        return dtos;
    }

    private async Task<PagedResult<LessonDto>> ToPagedResultAsync(IQueryable<Lesson> query, BaseSearchObject search, CancellationToken cancellationToken)
    {
        var count = await query.CountAsync(cancellationToken);

        query = search.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(l => l.StartsAtUtc)
            : query.OrderByDescending(l => l.StartsAtUtc);

        var lessons = await query
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LessonDto> { ResultList = await ToDtosAsync(lessons, cancellationToken), Count = count };
    }
}
