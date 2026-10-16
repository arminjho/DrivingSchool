namespace eAutoskola.Application.Reports;

/// <summary>Every property is optional: a missing date leaves that end of the period open, a
/// missing InstructorId means all instructors. Dates are whole UTC days, both inclusive.</summary>
public class LessonStatusReportFilter
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int? InstructorId { get; set; }
}

public class VehicleServiceReportFilter
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int? VehicleCategoryId { get; set; }
}
