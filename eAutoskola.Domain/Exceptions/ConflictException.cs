namespace eAutoskola.Domain.Exceptions;

/// <summary>A request conflicts with the current state of the resource (e.g. a duplicate
/// unique value, or a lesson slot taken between the overlap check and the save) -> HTTP 409.</summary>
public class ConflictException : EAutoskolaException
{
    public ConflictException(string message) : base(message)
    {
    }
}
