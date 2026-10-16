using eAutoskola.Domain.Entities;
using Mapster;

namespace eAutoskola.Application.Instructors;

public class InstructorMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, InstructorDto>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
            .Map(dest => dest.VehicleCategoryId, src => src.InstructorProfile!.VehicleCategoryId)
            .Map(dest => dest.VehicleCategoryName, src => src.InstructorProfile!.VehicleCategory!.Name);

        config.NewConfig<User, StudentSummaryDto>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
            .Map(dest => dest.VehicleCategoryId, src => src.StudentProfile!.VehicleCategoryId)
            .Map(dest => dest.VehicleCategoryName, src => src.StudentProfile!.VehicleCategory!.Name);
    }
}
