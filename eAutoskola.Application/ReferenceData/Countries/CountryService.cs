using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.ReferenceData.Countries;

public class CountryService : BaseCRUDService<CountryDto, CountrySearchObject, CountryRequest, CountryRequest, Country>
{
    public CountryService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<Country> ApplyFilter(IQueryable<Country> query, CountrySearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }
        return query;
    }

    protected override Task BeforeInsertAsync(Country entity, CountryRequest request, CancellationToken cancellationToken)
        => ValidateAsync(null, request, cancellationToken);

    protected override Task BeforeUpdateAsync(Country entity, CountryRequest request, CancellationToken cancellationToken)
        => ValidateAsync(entity, request, cancellationToken);

    protected override async Task BeforeDeleteAsync(Country entity, CancellationToken cancellationToken)
    {
        var cityCount = await Context.Cities.CountAsync(c => c.CountryId == entity.Id, cancellationToken);
        if (cityCount > 0)
        {
            throw new BusinessException(string.Format(Messages.CountryCannotBeDeleted, cityCount));
        }
    }

    private async Task ValidateAsync(Country? entity, CountryRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequireText(errors, nameof(request.Code), request.Code, 2, Messages.CodeLabel);
        FieldValidationRules.RequireText(errors, nameof(request.Name), request.Name, 100, Messages.NameLabel);

        var excludingId = entity?.Id ?? 0;
        if (errors.Count == 0 && await Context.Countries.AnyAsync(c => c.Code == request.Code && c.Id != excludingId, cancellationToken))
        {
            FieldValidationRules.Add(errors, nameof(request.Code), string.Format(Messages.CountryCodeAlreadyExists, request.Code));
        }

        FieldValidationRules.ThrowIfAny(errors);
    }
}
