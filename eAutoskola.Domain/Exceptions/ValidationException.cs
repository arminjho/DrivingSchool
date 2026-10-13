namespace eAutoskola.Domain.Exceptions;

/// <summary>
/// Field-level validation failure. Carries one or more field -> messages entries so the
/// client can render errors under the matching control, per upute.md §4.
/// </summary>
public class ValidationException : EAutoskolaException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("Dati podaci nisu validni.")
    {
        Errors = errors;
    }

    public ValidationException(string field, string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]> { [field] = [message] };
    }
}
