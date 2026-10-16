namespace eAutoskola.Domain.Entities;

public class Country
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }

    public ICollection<City> Cities { get; set; } = [];
}
