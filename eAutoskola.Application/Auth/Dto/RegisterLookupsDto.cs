using eAutoskola.Application.ReferenceData.Cities;
using eAutoskola.Application.ReferenceData.VehicleCategories;

namespace eAutoskola.Application.Auth.Dto;

/// <summary>The only two reference-data lists the registration form needs before the caller has
/// a token. A dedicated anonymous endpoint for exactly this, rather than opening the general
/// City/VehicleCategory CRUD endpoints, keeps [AllowAnonymous] confined to the auth endpoints
/// (upute.md §5: "dozvoljen isključivo na login/register endpointima").</summary>
public class RegisterLookupsDto
{
    public List<CityDto> Cities { get; set; } = [];
    public List<VehicleCategoryDto> VehicleCategories { get; set; } = [];
}
