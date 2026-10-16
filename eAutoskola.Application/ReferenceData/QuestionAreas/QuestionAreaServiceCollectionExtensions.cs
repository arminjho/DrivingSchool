using eAutoskola.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace eAutoskola.Application.ReferenceData.QuestionAreas;

public static class QuestionAreaServiceCollectionExtensions
{
    public static IServiceCollection AddQuestionAreaServices(this IServiceCollection services)
        => services.AddScoped<ICRUDService<QuestionAreaDto, QuestionAreaSearchObject, QuestionAreaRequest, QuestionAreaRequest>, QuestionAreaService>();
}
