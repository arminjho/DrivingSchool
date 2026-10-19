using eAutoskola.Domain.Resources;

namespace eAutoskola.Domain.Exceptions;

public class NotFoundException : EAutoskolaException
{
    public NotFoundException(string entityName, object key)
        : base(string.Format(Messages.EntityNotFound, entityName, key))
    {
    }
}
