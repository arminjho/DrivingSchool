using eAutoskola.Application.Reports;
using eAutoskola.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eAutoskola.API.Controllers;

/// <summary>Server-side PDF reports (QuestPDF) plus the same numbers as data, so the desktop
/// can chart a report beside the document without the two ever disagreeing.</summary>
[ApiController]
[Route("api/reports")]
[Authorize(Roles = Role.Admin)]
public class ReportController : ControllerBase
{
    private const string PdfContentType = "application/pdf";

    private readonly ReportService _service;

    public ReportController(ReportService service)
    {
        _service = service;
    }

    [HttpGet("lesson-status")]
    public async Task<ActionResult<LessonStatusReportData>> GetLessonStatus([FromQuery] LessonStatusReportFilter filter, CancellationToken cancellationToken)
        => Ok(await _service.GetLessonStatusReportAsync(filter, cancellationToken));

    [HttpGet("lesson-status/pdf")]
    public async Task<IActionResult> GetLessonStatusPdf([FromQuery] LessonStatusReportFilter filter, CancellationToken cancellationToken)
    {
        var pdf = await _service.GenerateLessonStatusReportPdfAsync(filter, cancellationToken);
        return File(pdf, PdfContentType, $"izvjestaj-status-casova-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }

    [HttpGet("vehicle-services")]
    public async Task<ActionResult<VehicleServiceReportData>> GetVehicleServices([FromQuery] VehicleServiceReportFilter filter, CancellationToken cancellationToken)
        => Ok(await _service.GetVehicleServiceReportAsync(filter, cancellationToken));

    [HttpGet("vehicle-services/pdf")]
    public async Task<IActionResult> GetVehicleServicesPdf([FromQuery] VehicleServiceReportFilter filter, CancellationToken cancellationToken)
    {
        var pdf = await _service.GenerateVehicleServiceReportPdfAsync(filter, cancellationToken);
        return File(pdf, PdfContentType, $"izvjestaj-vozila-i-servisi-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }
}
