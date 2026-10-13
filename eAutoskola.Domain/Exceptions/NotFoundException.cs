namespace eAutoskola.Domain.Exceptions;

public class NotFoundException : EAutoskolaException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} sa ID-om '{key}' nije pronađen/a.")
    {
    }
}
