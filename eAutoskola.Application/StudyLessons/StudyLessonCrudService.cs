using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.StudyLessons;

public class StudyLessonCrudService : BaseCRUDService<StudyLessonDto, StudyLessonSearchObject, StudyLessonRequest, StudyLessonRequest, StudyLesson>
{
    private readonly ICurrentUserService _currentUser;

    public StudyLessonCrudService(EAutoskolaDbContext context, ICurrentUserService currentUser) : base(context)
    {
        _currentUser = currentUser;
    }

    protected override IQueryable<StudyLesson> ApplyFilter(IQueryable<StudyLesson> query, StudyLessonSearchObject search)
    {
        query = query.Include(l => l.QuestionArea).Include(l => l.Sections);

        if (!string.IsNullOrWhiteSpace(search.Title))
        {
            query = query.Where(l => l.Title.Contains(search.Title));
        }
        if (search.QuestionAreaId.HasValue)
        {
            query = query.Where(l => l.QuestionAreaId == search.QuestionAreaId.Value);
        }
        return query;
    }

    protected override IQueryable<StudyLesson> GetByIdQuery(int id)
        => Context.StudyLessons.Include(l => l.QuestionArea).Include(l => l.Sections).Where(l => l.Id == id);

    /// <summary>"Sve lekcije" detail view. If the caller is a Student, opening the lesson
    /// records/updates their StudentStudyLesson row (prompt 2's explicit requirement).</summary>
    public async Task<StudyLessonDetailDto> GetDetailByIdAsync(int id, CancellationToken cancellationToken)
    {
        var lesson = await GetByIdQuery(id).OrderBy(l => l.Id).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(StudyLesson), id);

        if (_currentUser.IsInRole(Role.Student))
        {
            await RecordOpenAsync(id, cancellationToken);
        }

        var dto = lesson.Adapt<StudyLessonDetailDto>();
        dto.Sections = lesson.Sections.OrderBy(s => s.SortOrder).Adapt<List<StudyLessonSectionDto>>();
        return dto;
    }

    public async Task<PagedResult<StudentStudyLessonDto>> GetMyLessonsAsync(BaseSearchObject search, CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId;

        var query = Context.StudentStudyLessons
            .Include(s => s.StudyLesson!).ThenInclude(l => l.QuestionArea)
            .Include(s => s.StudyLesson!).ThenInclude(l => l.Sections)
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.LastOpenedAt);

        var count = await query.CountAsync(cancellationToken);
        var rows = await query.Skip((search.Page - 1) * search.PageSize).Take(search.PageSize).ToListAsync(cancellationToken);

        var result = rows.Select(r => new StudentStudyLessonDto
        {
            StudyLessonId = r.StudyLessonId,
            Title = r.StudyLesson!.Title,
            QuestionAreaName = r.StudyLesson.QuestionArea?.Name ?? string.Empty,
            ChapterLabel = r.StudyLesson.ChapterLabel,
            ReadingMinutes = r.StudyLesson.ReadingMinutes,
            SectionCount = r.StudyLesson.Sections.Count,
            OpenedAt = r.OpenedAt,
            LastOpenedAt = r.LastOpenedAt,
            ProgressPercent = r.ProgressPercent,
        }).ToList();

        return new PagedResult<StudentStudyLessonDto> { ResultList = result, Count = count };
    }

    private async Task RecordOpenAsync(int studyLessonId, CancellationToken cancellationToken)
    {
        var studentId = _currentUser.UserId;
        var existing = await Context.StudentStudyLessons
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.StudyLessonId == studyLessonId, cancellationToken);

        if (existing is null)
        {
            Context.StudentStudyLessons.Add(new StudentStudyLesson
            {
                StudentId = studentId, StudyLessonId = studyLessonId,
                OpenedAt = DateTime.UtcNow, LastOpenedAt = DateTime.UtcNow, ProgressPercent = 100,
            });
        }
        else
        {
            existing.LastOpenedAt = DateTime.UtcNow;
            existing.ProgressPercent = 100;
        }

        await Context.SaveChangesAsync(cancellationToken);
    }
}
