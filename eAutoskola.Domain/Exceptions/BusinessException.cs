namespace eAutoskola.Domain.Exceptions;

/// <summary>
/// A business-rule violation (e.g. double-booking, cancelling a paid lesson without a refund
/// flow, deleting a referenced record). Always safe to show <see cref="Exception.Message"/>
/// to the client as-is.
/// </summary>
public class BusinessException : EAutoskolaException
{
    public BusinessException(string message) : base(message)
    {
    }

    public BusinessException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
