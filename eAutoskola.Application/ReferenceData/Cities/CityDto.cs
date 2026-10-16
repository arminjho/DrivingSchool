namespace eAutoskola.Application.ReferenceData.Cities;

public class CityDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string PostalCode { get; set; }
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
}
