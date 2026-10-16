using eAutoskola.Domain.Entities;
using Mapster;

namespace eAutoskola.Application.Tests;

public class TestMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Test, TestDto>()
            .Map(dest => dest.QuestionCount, src => src.Questions.Count);
    }
}
