using eAutoskola.Domain.Entities;
using Mapster;

namespace eAutoskola.Application.Lessons;

public class LessonMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Lesson, LessonDto>()
            .Map(dest => dest.StudentFullName, src => src.Student != null ? $"{src.Student.FirstName} {src.Student.LastName}" : string.Empty)
            .Map(dest => dest.InstructorFullName, src => src.Instructor != null ? $"{src.Instructor.FirstName} {src.Instructor.LastName}" : string.Empty);
    }
}
