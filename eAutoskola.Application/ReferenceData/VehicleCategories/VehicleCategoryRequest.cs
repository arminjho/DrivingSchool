namespace eAutoskola.Application.ReferenceData.VehicleCategories;

public class VehicleCategoryRequest
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
