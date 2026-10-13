namespace eAutoskola.Domain.Exceptions;

public abstract class EAutoskolaException : Exception
{
    protected EAutoskolaException(string message) : base(message)
    {
    }

    protected EAutoskolaException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
