namespace eAutoskola.Domain.Entities;

/// <summary>
/// Vehicle maintenance type (Redovni servis / Zamjena ulja / Tehnički pregled). Separate from
/// ServiceType (the driving-lesson price catalogue) - different domain, same English word.
/// </summary>
public class VehicleServiceType
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
