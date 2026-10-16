namespace eAutoskola.Application.ReferenceData.Cities;

public class CityRequest
{
    public required string Name { get; set; }
    public required string PostalCode { get; set; }
    public int CountryId { get; set; }
}
