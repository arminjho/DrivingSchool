using eAutoskola.Application.Common;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Security;
using eAutoskola.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Application.Reports;

/// <summary>
/// The two admin reports. For each, one method gathers the data and another renders it to PDF
/// from that same data, so the on-screen chart and the printed document can't disagree.
/// </summary>
public class ReportService
{
    private readonly EAutoskolaDbContext _context;

    public ReportService(EAutoskolaDbContext context)
    {
        _context = context;
    }

    // --- Lesson status report ---

    public async Task<LessonStatusReportData> GetLessonStatusReportAsync(LessonStatusReportFilter filter, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateRange(errors, filter.DateFrom, filter.DateTo);

        string? instructorName = null;
        if (filter.InstructorId is { } instructorId)
        {
            instructorName = await _context.Users.AsNoTracking()
                .Where(u => u.Id == instructorId && u.Role == Role.Instructor)
                .Select(u => u.FirstName + " " + u.LastName)
                .FirstOrDefaultAsync(cancellationToken);
            if (instructorName is null)
            {
                FieldValidationRules.Add(errors, nameof(filter.InstructorId), Messages.ReportInstructorNotFound);
            }
        }
        FieldValidationRules.ThrowIfAny(errors);

        // Lessons are history: an instructor who was removed since still taught them, so the
        // soft-delete filter on User must not silently drop their lessons from the counts.
        var query = _context.Lessons.IgnoreQueryFilters().AsNoTracking().AsQueryable();
        if (filter.DateFrom is { } from)
        {
            var fromUtc = StartOfDayUtc(from);
            query = query.Where(l => l.StartsAtUtc >= fromUtc);
        }
        if (filter.DateTo is { } to)
        {
            var toExclusiveUtc = StartOfDayUtc(to.AddDays(1));
            query = query.Where(l => l.StartsAtUtc < toExclusiveUtc);
        }
        if (filter.InstructorId is { } onlyInstructorId)
        {
            query = query.Where(l => l.InstructorId == onlyInstructorId);
        }

        var groups = await query
            .GroupBy(l => new { l.InstructorId, l.Instructor!.FirstName, l.Instructor.LastName })
            .Select(g => new
            {
                g.Key.InstructorId,
                g.Key.FirstName,
                g.Key.LastName,
                Completed = g.Count(l => l.Status == LessonStatus.Completed),
                Scheduled = g.Count(l => l.Status == LessonStatus.Pending || l.Status == LessonStatus.Confirmed),
                Cancelled = g.Count(l => l.Status == LessonStatus.Cancelled),
            })
            .ToListAsync(cancellationToken);

        var rows = groups
            .Select(g => new LessonStatusReportRow
            {
                InstructorId = g.InstructorId,
                InstructorName = $"{g.FirstName} {g.LastName}",
                Completed = g.Completed,
                Scheduled = g.Scheduled,
                Cancelled = g.Cancelled,
            })
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.InstructorName)
            .ToList();

        return new LessonStatusReportData
        {
            DateFrom = filter.DateFrom,
            DateTo = filter.DateTo,
            InstructorId = filter.InstructorId,
            InstructorName = instructorName,
            GeneratedAtUtc = DateTime.UtcNow,
            Rows = rows,
        };
    }

    public async Task<byte[]> GenerateLessonStatusReportPdfAsync(LessonStatusReportFilter filter, CancellationToken cancellationToken)
        => LessonStatusReportPdf.Render(await GetLessonStatusReportAsync(filter, cancellationToken));

    // --- Vehicle & service report ---

    public async Task<VehicleServiceReportData> GetVehicleServiceReportAsync(VehicleServiceReportFilter filter, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        ValidateRange(errors, filter.DateFrom, filter.DateTo);

        string? categoryName = null;
        if (filter.VehicleCategoryId is { } categoryId)
        {
            categoryName = await _context.VehicleCategories.AsNoTracking()
                .Where(c => c.Id == categoryId)
                .Select(c => c.Code + " - " + c.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (categoryName is null)
            {
                FieldValidationRules.Add(errors, nameof(filter.VehicleCategoryId), Messages.ReportVehicleCategoryNotFound);
            }
        }
        FieldValidationRules.ThrowIfAny(errors);

        var vehicleQuery = _context.Vehicles.AsNoTracking().AsQueryable();
        var serviceQuery = _context.VehicleServices.AsNoTracking().AsQueryable();
        if (filter.VehicleCategoryId is { } onlyCategoryId)
        {
            vehicleQuery = vehicleQuery.Where(v => v.VehicleCategoryId == onlyCategoryId);
            serviceQuery = serviceQuery.Where(s => s.Vehicle!.VehicleCategoryId == onlyCategoryId);
        }
        if (filter.DateFrom is { } from)
        {
            var fromUtc = StartOfDayUtc(from);
            serviceQuery = serviceQuery.Where(s => s.ServiceDate >= fromUtc);
        }
        if (filter.DateTo is { } to)
        {
            var toExclusiveUtc = StartOfDayUtc(to.AddDays(1));
            serviceQuery = serviceQuery.Where(s => s.ServiceDate < toExclusiveUtc);
        }

        // Two queries for the whole report - the fleet, then every service in scope - joined
        // in memory by vehicle id, never one query per vehicle. Vehicles without a service in
        // the period are kept: the report is also an overview of the fleet's state.
        var vehicles = await vehicleQuery
            .OrderBy(v => v.VehicleCategory!.Code).ThenBy(v => v.RegistrationNumber)
            .Select(v => new VehicleServiceReportVehicle
            {
                VehicleId = v.Id,
                RegistrationNumber = v.RegistrationNumber,
                Make = v.Make,
                Model = v.Model,
                CategoryCode = v.VehicleCategory!.Code,
                CategoryName = v.VehicleCategory.Name,
                ManufactureYear = v.ManufactureYear,
                Status = v.Status,
            })
            .ToListAsync(cancellationToken);

        var services = await serviceQuery
            .OrderBy(s => s.ServiceDate).ThenBy(s => s.Id)
            .Select(s => new
            {
                s.VehicleId,
                Entry = new VehicleServiceReportEntry
                {
                    ServiceDate = s.ServiceDate,
                    ServiceTypeName = s.VehicleServiceType!.Name,
                    Description = s.Description,
                    Status = s.Status,
                    Cost = s.Cost,
                },
            })
            .ToListAsync(cancellationToken);

        var servicesByVehicle = services.ToLookup(s => s.VehicleId, s => s.Entry);
        foreach (var vehicle in vehicles)
        {
            vehicle.Services = servicesByVehicle[vehicle.VehicleId].ToList();
        }

        return new VehicleServiceReportData
        {
            DateFrom = filter.DateFrom,
            DateTo = filter.DateTo,
            VehicleCategoryId = filter.VehicleCategoryId,
            VehicleCategoryName = categoryName,
            GeneratedAtUtc = DateTime.UtcNow,
            Vehicles = vehicles,
        };
    }

    public async Task<byte[]> GenerateVehicleServiceReportPdfAsync(VehicleServiceReportFilter filter, CancellationToken cancellationToken)
        => VehicleServiceReportPdf.Render(await GetVehicleServiceReportAsync(filter, cancellationToken));

    // --- shared ---

    private static void ValidateRange(IDictionary<string, string[]> errors, DateOnly? dateFrom, DateOnly? dateTo)
    {
        if (dateFrom is { } from && dateTo is { } to && to < from)
        {
            FieldValidationRules.Add(errors, nameof(LessonStatusReportFilter.DateTo), Messages.ReportDateRangeInvalid);
        }
    }

    private static DateTime StartOfDayUtc(DateOnly date) => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
