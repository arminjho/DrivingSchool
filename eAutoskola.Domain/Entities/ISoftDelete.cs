namespace eAutoskola.Domain.Entities;

/// <summary>
/// Marker interface for entities deleted via soft-delete (IsDeleted/DeletedAt) instead of a
/// hard DELETE, so business history survives. BaseCommandService checks this interface to
/// decide whether Delete soft- or hard-deletes (upute.md §3.1).
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
