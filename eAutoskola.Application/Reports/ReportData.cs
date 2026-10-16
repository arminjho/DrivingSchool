using System.Text.Json.Serialization;
using eAutoskola.Domain.Enums;

namespace eAutoskola.Application.Reports;

/// <summary>The numbers behind the lesson status report - the PDF and the data endpoint are
/// both rendered from this, so a chart on screen can never disagree with the printed document.</summary>
public class LessonStatusReportData
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int? InstructorId { get; set; }

    /// <summary>Null when the report covers every instructor.</summary>
    public string? InstructorName { get; set; }

    public DateTime GeneratedAtUtc { get; set; }
    public List<LessonStatusReportRow> Rows { get; set; } = [];

    public int TotalCompleted => Rows.Sum(r => r.Completed);
    public int TotalScheduled => Rows.Sum(r => r.Scheduled);
    public int TotalCancelled => Rows.Sum(r => r.Cancelled);
    public int TotalLessons => Rows.Sum(r => r.Total);
}

public class LessonStatusReportRow
{
    public int InstructorId { get; set; }
    public required string InstructorName { get; set; }
    public int Completed { get; set; }

    /// <summary>Pending + Confirmed.</summary>
    public int Scheduled { get; set; }

    public int Cancelled { get; set; }
    public int Total => Completed + Scheduled + Cancelled;
}

public class VehicleServiceReportData
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int? VehicleCategoryId { get; set; }

    /// <summary>Null when the report covers every category.</summary>
    public string? VehicleCategoryName { get; set; }

    public DateTime GeneratedAtUtc { get; set; }
    public List<VehicleServiceReportVehicle> Vehicles { get; set; } = [];

    public int TotalVehicles => Vehicles.Count;
    public int TotalServices => Vehicles.Sum(v => v.Services.Count);
    public decimal TotalCost => Vehicles.Sum(v => v.TotalCost);

    public List<VehicleServiceReportCategory> Categories => Vehicles
        .GroupBy(v => new { v.CategoryCode, v.CategoryName })
        .OrderBy(g => g.Key.CategoryCode)
        .Select(g => new VehicleServiceReportCategory
        {
            CategoryCode = g.Key.CategoryCode,
            CategoryName = g.Key.CategoryName,
            VehicleCount = g.Count(),
            ServiceCount = g.Sum(v => v.Services.Count),
            TotalCost = g.Sum(v => v.TotalCost),
        })
        .ToList();

    public int CountServices(VehicleServiceStatus status) => Vehicles.Sum(v => v.Services.Count(s => s.Status == status));
}

public class VehicleServiceReportVehicle
{
    public int VehicleId { get; set; }
    public required string RegistrationNumber { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public required string CategoryCode { get; set; }
    public required string CategoryName { get; set; }
    public int ManufactureYear { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehicleStatus Status { get; set; }

    public List<VehicleServiceReportEntry> Services { get; set; } = [];
    public decimal TotalCost => Services.Sum(s => s.Cost);
}

public class VehicleServiceReportEntry
{
    public DateTime ServiceDate { get; set; }
    public required string ServiceTypeName { get; set; }
    public required string Description { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehicleServiceStatus Status { get; set; }

    public decimal Cost { get; set; }
}

public class VehicleServiceReportCategory
{
    public required string CategoryCode { get; set; }
    public required string CategoryName { get; set; }
    public int VehicleCount { get; set; }
    public int ServiceCount { get; set; }
    public decimal TotalCost { get; set; }
}
