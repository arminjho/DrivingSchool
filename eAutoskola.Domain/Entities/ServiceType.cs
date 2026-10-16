namespace eAutoskola.Domain.Entities;

/// <summary>
/// Price catalogue for training services (Teorijska obuka / Praktična vožnja / Administracija i
/// ispit / Dodatni čas vožnje) - snapshotted into Charge.Amount. Not to be confused with
/// VehicleServiceType (vehicle maintenance type).
/// </summary>
public class ServiceType
{
    public int Id { get; set; }

    /// <summary>Stable identifier the payment logic looks rows up by (Name is admin-editable
    /// display text) - same idea as RequestStatus.Code.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }
    public decimal Price { get; set; }
}
