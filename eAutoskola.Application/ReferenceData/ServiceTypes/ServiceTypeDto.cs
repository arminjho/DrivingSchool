namespace eAutoskola.Application.ReferenceData.ServiceTypes;

public class ServiceTypeDto
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
}
