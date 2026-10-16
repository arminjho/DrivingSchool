using System.Text;
using eAutoskola.Application.Reports;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;

namespace eAutoskola.Tests.Reports;

public class ReportServiceTests
{
    private static ReportService CreateService(ReportTestFixture f) => new(f.Context);

    private static DateOnly D(int year, int month, int day) => new(year, month, day);

    // --- lesson status report ---

    [Fact]
    public async Task LessonStatus_GroupsByInstructor_AndScheduledMeansPendingPlusConfirmed()
    {
        var f = new ReportTestFixture();
        var student = f.AddStudent();
        var damir = f.AddInstructor("Damir", "Kovačević");
        var sanja = f.AddInstructor("Sanja", "Perić");
        f.AddLesson(student, damir, ReportTestFixture.Utc(2026, 3, 1), LessonStatus.Completed);
        f.AddLesson(student, damir, ReportTestFixture.Utc(2026, 3, 2), LessonStatus.Completed);
        f.AddLesson(student, damir, ReportTestFixture.Utc(2026, 3, 3), LessonStatus.Pending);
        f.AddLesson(student, damir, ReportTestFixture.Utc(2026, 3, 4), LessonStatus.Confirmed);
        f.AddLesson(student, damir, ReportTestFixture.Utc(2026, 3, 5), LessonStatus.Cancelled);
        f.AddLesson(student, sanja, ReportTestFixture.Utc(2026, 3, 6), LessonStatus.Completed);

        var report = await CreateService(f).GetLessonStatusReportAsync(new LessonStatusReportFilter(), default);

        Assert.Equal(["Damir Kovačević", "Sanja Perić"], report.Rows.Select(r => r.InstructorName)); // busiest first
        var damirRow = report.Rows[0];
        Assert.Equal((2, 2, 1, 5), (damirRow.Completed, damirRow.Scheduled, damirRow.Cancelled, damirRow.Total));
        Assert.Equal((3, 2, 1, 6), (report.TotalCompleted, report.TotalScheduled, report.TotalCancelled, report.TotalLessons));
        Assert.Null(report.InstructorName);
    }

    [Fact]
    public async Task LessonStatus_DateRange_IsInclusiveOfBothWholeDays()
    {
        var f = new ReportTestFixture();
        var student = f.AddStudent();
        var instructor = f.AddInstructor();
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 2, 28, 23), LessonStatus.Completed);  // before
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 3, 1, 0), LessonStatus.Completed);    // first day, first hour
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 3, 31, 23), LessonStatus.Completed);  // last day, last hour
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 4, 1, 0), LessonStatus.Completed);    // after

        var report = await CreateService(f).GetLessonStatusReportAsync(
            new LessonStatusReportFilter { DateFrom = D(2026, 3, 1), DateTo = D(2026, 3, 31) }, default);

        Assert.Equal(2, report.TotalLessons);
    }

    [Fact]
    public async Task LessonStatus_OpenEndedPeriod_AppliesOnlyTheGivenBound()
    {
        var f = new ReportTestFixture();
        var student = f.AddStudent();
        var instructor = f.AddInstructor();
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 1, 10), LessonStatus.Completed);
        f.AddLesson(student, instructor, ReportTestFixture.Utc(2026, 6, 10), LessonStatus.Completed);

        var service = CreateService(f);
        var fromOnly = await service.GetLessonStatusReportAsync(new LessonStatusReportFilter { DateFrom = D(2026, 3, 1) }, default);
        var toOnly = await service.GetLessonStatusReportAsync(new LessonStatusReportFilter { DateTo = D(2026, 3, 1) }, default);

        Assert.Equal(1, fromOnly.TotalLessons);
        Assert.Equal(1, toOnly.TotalLessons);
    }

    [Fact]
    public async Task LessonStatus_InstructorFilter_ReturnsOnlyThatInstructor_AndTheirName()
    {
        var f = new ReportTestFixture();
        var student = f.AddStudent();
        var damir = f.AddInstructor("Damir", "Kovačević");
        var sanja = f.AddInstructor("Sanja", "Perić");
        f.AddLesson(student, damir, ReportTestFixture.Utc(2026, 3, 1), LessonStatus.Completed);
        f.AddLesson(student, sanja, ReportTestFixture.Utc(2026, 3, 2), LessonStatus.Completed);

        var report = await CreateService(f).GetLessonStatusReportAsync(new LessonStatusReportFilter { InstructorId = sanja.Id }, default);

        Assert.Equal("Sanja Perić", report.InstructorName);
        Assert.Equal("Sanja Perić", Assert.Single(report.Rows).InstructorName);
    }

    [Fact]
    public async Task LessonStatus_RemovedInstructorsLessonsStillCount()
    {
        var f = new ReportTestFixture();
        var student = f.AddStudent();
        var removed = f.AddInstructor("Stari", "Instruktor", isDeleted: true);
        f.AddLesson(student, removed, ReportTestFixture.Utc(2026, 3, 1), LessonStatus.Completed);

        var report = await CreateService(f).GetLessonStatusReportAsync(new LessonStatusReportFilter(), default);

        Assert.Equal("Stari Instruktor", Assert.Single(report.Rows).InstructorName);
        Assert.Equal(1, report.TotalCompleted);
    }

    [Fact]
    public async Task LessonStatus_NoMatchingLessons_GivesNoRows()
    {
        var f = new ReportTestFixture();
        var instructor = f.AddInstructor();

        var report = await CreateService(f).GetLessonStatusReportAsync(new LessonStatusReportFilter { InstructorId = instructor.Id }, default);

        Assert.Empty(report.Rows);
        Assert.Equal(0, report.TotalLessons);
    }

    [Fact]
    public async Task LessonStatus_EndBeforeStart_IsAFieldValidationError()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => CreateService(new ReportTestFixture()).GetLessonStatusReportAsync(
            new LessonStatusReportFilter { DateFrom = D(2026, 3, 10), DateTo = D(2026, 3, 1) }, default));

        Assert.Contains("DateTo", ex.Errors.Keys);
        Assert.Contains("DD.MM.GGGG", ex.Errors["DateTo"][0]);
    }

    [Fact]
    public async Task LessonStatus_UnknownOrNonInstructor_IsAFieldValidationError()
    {
        var f = new ReportTestFixture();
        var student = f.AddStudent();
        var service = CreateService(f);

        var unknown = await Assert.ThrowsAsync<ValidationException>(() => service.GetLessonStatusReportAsync(new LessonStatusReportFilter { InstructorId = 999 }, default));
        var notAnInstructor = await Assert.ThrowsAsync<ValidationException>(() => service.GetLessonStatusReportAsync(new LessonStatusReportFilter { InstructorId = student.Id }, default));

        Assert.Contains("InstructorId", unknown.Errors.Keys);
        Assert.Contains("InstructorId", notAnInstructor.Errors.Keys);
    }

    // --- vehicle & service report ---

    [Fact]
    public async Task VehicleServices_ListsEveryVehicleWithItsHistoryAndCosts()
    {
        var f = new ReportTestFixture();
        var polo = f.AddVehicle("E22-A-123", f.CategoryB);
        var sprinter = f.AddVehicle("E31-T-321", f.CategoryC);
        var spare = f.AddVehicle("E99-Z-999", f.CategoryB);
        f.AddService(polo, ReportTestFixture.Utc(2026, 4, 10), 120m, VehicleServiceStatus.Completed);
        f.AddService(polo, ReportTestFixture.Utc(2026, 2, 2), 60m, VehicleServiceStatus.Completed, f.ServiceTypeOil);
        f.AddService(sprinter, ReportTestFixture.Utc(2026, 5, 20), 300m, VehicleServiceStatus.Scheduled);

        var report = await CreateService(f).GetVehicleServiceReportAsync(new VehicleServiceReportFilter(), default);

        Assert.Equal(["E22-A-123", "E99-Z-999", "E31-T-321"], report.Vehicles.Select(v => v.RegistrationNumber)); // by category, then plate
        var poloRow = report.Vehicles[0];
        Assert.Equal([ReportTestFixture.Utc(2026, 2, 2), ReportTestFixture.Utc(2026, 4, 10)], poloRow.Services.Select(s => s.ServiceDate)); // oldest first
        Assert.Equal(180m, poloRow.TotalCost);
        Assert.Empty(report.Vehicles.Single(v => v.VehicleId == spare.Id).Services); // fleet overview keeps idle vehicles
        Assert.Equal((3, 3, 480m), (report.TotalVehicles, report.TotalServices, report.TotalCost));
        Assert.Equal(2, report.CountServices(VehicleServiceStatus.Completed));
        Assert.Equal(1, report.CountServices(VehicleServiceStatus.Scheduled));
        Assert.Equal(0, report.CountServices(VehicleServiceStatus.InProgress));
    }

    [Fact]
    public async Task VehicleServices_CategoryFilter_LimitsVehiclesServicesAndCategoryName()
    {
        var f = new ReportTestFixture();
        var polo = f.AddVehicle("E22-A-123", f.CategoryB);
        var sprinter = f.AddVehicle("E31-T-321", f.CategoryC);
        f.AddService(polo, ReportTestFixture.Utc(2026, 4, 10), 120m);
        f.AddService(sprinter, ReportTestFixture.Utc(2026, 5, 20), 300m);

        var report = await CreateService(f).GetVehicleServiceReportAsync(new VehicleServiceReportFilter { VehicleCategoryId = f.CategoryC.Id }, default);

        Assert.Equal("C - Teretna vozila", report.VehicleCategoryName);
        Assert.Equal("E31-T-321", Assert.Single(report.Vehicles).RegistrationNumber);
        Assert.Equal(300m, report.TotalCost);
    }

    [Fact]
    public async Task VehicleServices_DateRange_FiltersServicesButNotVehicles()
    {
        var f = new ReportTestFixture();
        var polo = f.AddVehicle("E22-A-123", f.CategoryB);
        f.AddService(polo, ReportTestFixture.Utc(2026, 1, 15), 50m);
        f.AddService(polo, ReportTestFixture.Utc(2026, 4, 10), 120m);
        f.AddService(polo, ReportTestFixture.Utc(2026, 4, 30), 70m);
        f.AddService(polo, ReportTestFixture.Utc(2026, 8, 1), 90m);

        var report = await CreateService(f).GetVehicleServiceReportAsync(
            new VehicleServiceReportFilter { DateFrom = D(2026, 4, 1), DateTo = D(2026, 4, 30) }, default);

        var vehicle = Assert.Single(report.Vehicles);
        Assert.Equal(2, vehicle.Services.Count);
        Assert.Equal(190m, report.TotalCost);
    }

    [Fact]
    public async Task VehicleServices_RemovedVehiclesAreNotPartOfTheFleetReport()
    {
        var f = new ReportTestFixture();
        f.AddVehicle("E22-A-123", f.CategoryB);
        var removed = f.AddVehicle("E00-X-000", f.CategoryB, isDeleted: true);
        f.AddService(removed, ReportTestFixture.Utc(2026, 4, 10), 999m);

        var report = await CreateService(f).GetVehicleServiceReportAsync(new VehicleServiceReportFilter(), default);

        Assert.Equal("E22-A-123", Assert.Single(report.Vehicles).RegistrationNumber);
        Assert.Equal(0m, report.TotalCost);
    }

    [Fact]
    public async Task VehicleServices_CategorySummary_GroupsVehiclesServicesAndCost()
    {
        var f = new ReportTestFixture();
        var polo = f.AddVehicle("E22-A-123", f.CategoryB);
        var focus = f.AddVehicle("E88-M-789", f.CategoryB);
        var sprinter = f.AddVehicle("E31-T-321", f.CategoryC);
        f.AddService(polo, ReportTestFixture.Utc(2026, 4, 10), 100m);
        f.AddService(focus, ReportTestFixture.Utc(2026, 4, 11), 50m);
        f.AddService(sprinter, ReportTestFixture.Utc(2026, 4, 12), 300m);

        var categories = (await CreateService(f).GetVehicleServiceReportAsync(new VehicleServiceReportFilter(), default)).Categories;

        Assert.Equal(["B", "C"], categories.Select(c => c.CategoryCode));
        Assert.Equal((2, 2, 150m), (categories[0].VehicleCount, categories[0].ServiceCount, categories[0].TotalCost));
        Assert.Equal((1, 1, 300m), (categories[1].VehicleCount, categories[1].ServiceCount, categories[1].TotalCost));
    }

    [Fact]
    public async Task VehicleServices_BadFilters_AreFieldValidationErrors()
    {
        var service = CreateService(new ReportTestFixture());

        var badRange = await Assert.ThrowsAsync<ValidationException>(() => service.GetVehicleServiceReportAsync(
            new VehicleServiceReportFilter { DateFrom = D(2026, 5, 1), DateTo = D(2026, 4, 1) }, default));
        var badCategory = await Assert.ThrowsAsync<ValidationException>(() => service.GetVehicleServiceReportAsync(
            new VehicleServiceReportFilter { VehicleCategoryId = 999 }, default));

        Assert.Contains("DateTo", badRange.Errors.Keys);
        Assert.Contains("VehicleCategoryId", badCategory.Errors.Keys);
    }

    // --- PDF output ---

    private static void AssertIsPdf(byte[] pdf)
    {
        Assert.True(pdf.Length > 1000, "PDF should not be trivially small");
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
        Assert.Contains("%%EOF", Encoding.ASCII.GetString(pdf, pdf.Length - 32, 32));
    }

    [Fact]
    public async Task LessonStatus_Pdf_IsAValidPdfWithData()
    {
        var f = new ReportTestFixture();
        var student = f.AddStudent();
        var damir = f.AddInstructor("Damir", "Kovačević");
        var sanja = f.AddInstructor("Sanja", "Perić");
        f.AddLesson(student, damir, ReportTestFixture.Utc(2026, 3, 1), LessonStatus.Completed);
        f.AddLesson(student, damir, ReportTestFixture.Utc(2026, 3, 2), LessonStatus.Cancelled);
        f.AddLesson(student, sanja, ReportTestFixture.Utc(2026, 3, 3), LessonStatus.Confirmed);

        var pdf = await CreateService(f).GenerateLessonStatusReportPdfAsync(
            new LessonStatusReportFilter { DateFrom = D(2026, 1, 1), DateTo = D(2026, 12, 31), InstructorId = damir.Id }, default);

        AssertIsPdf(pdf);
    }

    [Fact]
    public async Task LessonStatus_Pdf_WithNoLessons_IsStillAValidPdf()
        => AssertIsPdf(await CreateService(new ReportTestFixture()).GenerateLessonStatusReportPdfAsync(new LessonStatusReportFilter(), default));

    [Fact]
    public async Task VehicleServices_Pdf_IsAValidPdfWithData()
    {
        var f = new ReportTestFixture();
        var polo = f.AddVehicle("E22-A-123", f.CategoryB);
        f.AddVehicle("E31-T-321", f.CategoryC);
        for (var i = 0; i < 40; i++)
        {
            f.AddService(polo, ReportTestFixture.Utc(2026, 1, 1).AddDays(i), 10m + i, (VehicleServiceStatus)(i % 3));
        }

        var pdf = await CreateService(f).GenerateVehicleServiceReportPdfAsync(new VehicleServiceReportFilter { DateFrom = D(2026, 1, 1) }, default);

        AssertIsPdf(pdf); // 40 rows: also exercises a table that spans pages
    }

    [Fact]
    public async Task VehicleServices_Pdf_WithNoVehicles_IsStillAValidPdf()
        => AssertIsPdf(await CreateService(new ReportTestFixture()).GenerateVehicleServiceReportPdfAsync(new VehicleServiceReportFilter(), default));
}
