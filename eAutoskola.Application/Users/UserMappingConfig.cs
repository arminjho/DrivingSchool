using eAutoskola.Domain.Entities;
using Mapster;

namespace eAutoskola.Application.Users;

/// <summary>
/// FullName is a computed concatenation (not a renamed/nested property), and VehicleCategoryId
/// lives on StudentProfile/InstructorProfile, not User itself - Mapster's convention mapping
/// can flatten a nested property path automatically but can't compute either of these, so they
/// need an explicit mapping.
/// </summary>
public class UserMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserDto>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
            .Map(dest => dest.VehicleCategoryId, src =>
                src.StudentProfile != null ? src.StudentProfile.VehicleCategoryId :
                src.InstructorProfile != null ? src.InstructorProfile.VehicleCategoryId : (int?)null);
    }
}
