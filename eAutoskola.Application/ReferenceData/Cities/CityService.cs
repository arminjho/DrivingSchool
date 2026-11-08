using eAutoskola.Application.Common;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Resources;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.ReferenceData.Cities;

public class CityService : BaseCRUDService<CityDto, CitySearchObject, CityRequest, CityRequest, City>
{
    public CityService(EAutoskolaDbContext context) : base(context)
    {
    }

    protected override IQueryable<City> ApplyFilter(IQueryable<City> query, CitySearchObject search)
    {
        query = query.Include(c => c.Country);

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }
        if (search.CountryId.HasValue)
        {
            query = query.Where(c => c.CountryId == search.CountryId.Value);
        }
        return query;
    }

    protected override IQueryable<City> GetByIdQuery(int id)
        => Context.Cities.Include(c => c.Country).Where(c => c.Id == id);

    protected override Task BeforeInsertAsync(City entity, CityRequest request, CancellationToken cancellationToken)
        => ValidateAsync(request, cancellationToken);

    protected override Task BeforeUpdateAsync(City entity, CityRequest request, CancellationToken cancellationToken)
        => ValidateAsync(request, cancellationToken);

    protected override async Task BeforeDeleteAsync(City entity, CancellationToken cancellationToken)
    {
        var userCount = await Context.Users.CountAsync(u => u.CityId == entity.Id, cancellationToken);
        if (userCount > 0)
        {
            throw new BusinessException(string.Format(Messages.CityCannotBeDeleted, userCount));
        }
    }

    private async Task ValidateAsync(CityRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        FieldValidationRules.RequireText(errors, nameof(request.Name), request.Name, 100, Messages.NameLabel);
        FieldValidationRules.RequireText(errors, nameof(request.PostalCode), request.PostalCode, 10, Messages.PostalCodeLabel);

        if (!await Context.Countries.AnyAsync(c => c.Id == request.CountryId, cancellationToken))
        {
            FieldValidationRules.Add(errors, nameof(request.CountryId), Messages.CountryNotFound);
        }

        FieldValidationRules.ThrowIfAny(errors);
    }
}
