using eAutoskola.Domain.Entities;
using Mapster;

namespace eAutoskola.Application.StudyLessons;

public class StudyLessonMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<StudyLesson, StudyLessonDto>()
            .Map(dest => dest.SectionCount, src => src.Sections.Count);

        config.NewConfig<StudyLesson, StudyLessonDetailDto>()
            .Map(dest => dest.SectionCount, src => src.Sections.Count);
    }
}
