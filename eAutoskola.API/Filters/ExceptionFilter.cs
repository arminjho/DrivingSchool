using System.Net;
using eAutoskola.Domain.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;

namespace eAutoskola.API.Filters;

/// <summary>
/// Central exception -> HTTP status mapping. Custom EAutoskolaException subclasses never leak
/// a stack trace; anything unhandled returns a generic message in non-Development
/// environments. Mirrors REFERENCE_ANALYSIS.md §2.5 / upute.md §3.4.
/// </summary>
public class ExceptionFilter : Microsoft.AspNetCore.Mvc.Filters.ExceptionFilterAttribute
{
    private const string GenericServerErrorMessage = "Došlo je do neočekivane greške. Pokušajte ponovo kasnije.";

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(IWebHostEnvironment environment, ILogger<ExceptionFilter> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {
        var (statusCode, errors) = context.Exception switch
        {
            ValidationException validation => (HttpStatusCode.BadRequest, validation.Errors),
            BusinessException business => (HttpStatusCode.BadRequest, SingleError(business.Message)),
            NotFoundException notFound => (HttpStatusCode.NotFound, SingleError(notFound.Message)),
            ForbiddenException forbidden => (HttpStatusCode.Forbidden, SingleError(forbidden.Message)),
            AuthenticationException authentication => (HttpStatusCode.Unauthorized, SingleError(authentication.Message)),
            _ => (HttpStatusCode.InternalServerError, SingleError(GenericServerErrorMessage)),
        };

        if (context.Exception is EAutoskolaException)
        {
            _logger.LogWarning(context.Exception, "Handled application exception");
        }
        else
        {
            _logger.LogError(context.Exception, "Unhandled exception");
        }

        context.HttpContext.Response.StatusCode = (int)statusCode;

        object body = _environment.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError
            ? new { errors, detail = context.Exception.ToString() }
            : new { errors };

        context.Result = new Microsoft.AspNetCore.Mvc.JsonResult(body);
        context.ExceptionHandled = true;
    }

    private static Dictionary<string, string[]> SingleError(string message)
        => new() { [""] = [message] };
}
