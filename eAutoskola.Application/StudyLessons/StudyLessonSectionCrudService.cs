using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.StudyLessons;

public class StudyLessonSectionCrudService : BaseCRUDService<StudyLessonSectionDto, StudyLessonSectionSearchObject, StudyLessonSectionRequest, StudyLessonSectionRequest, StudyLessonSection>
{
    public StudyLessonSectionCrudService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<StudyLessonSection> ApplyFilter(IQueryable<StudyLessonSection> query, StudyLessonSectionSearchObject search)
    {
        if (search.StudyLessonId.HasValue)
        {
            query = query.Where(s => s.StudyLessonId == search.StudyLessonId.Value);
        }
        return query;
    }
}
