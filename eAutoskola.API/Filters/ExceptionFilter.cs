using System.Net;
using System.Security.Claims;
using eAutoskola.Domain.Resources;
using eAutoskola.Domain.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace eAutoskola.API.Filters;

/// <summary>
/// Central exception -> HTTP status mapping. Response body is {message, errors}: message is a
/// single Bosnian summary, errors is a field -> messages dictionary for form validation.
/// Custom EAutoskolaException subclasses never leak a stack trace; anything unhandled returns
/// a generic Bosnian message in non-Development environments while the full exception (with
/// request path, user id, and correlation id) is always logged in English.
/// Mirrors REFERENCE_ANALYSIS.md §2.5 / upute.md §3.4.
/// </summary>
public class ExceptionFilter : ExceptionFilterAttribute
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(IWebHostEnvironment environment, ILogger<ExceptionFilter> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {
        var (statusCode, message, errors) = context.Exception switch
        {
            ValidationException validation => (HttpStatusCode.BadRequest, validation.Message, validation.Errors),
            BusinessException business => (HttpStatusCode.BadRequest, business.Message, null),
            ConflictException conflict => (HttpStatusCode.Conflict, conflict.Message, null),
            NotFoundException notFound => (HttpStatusCode.NotFound, notFound.Message, null),
            ForbiddenException forbidden => (HttpStatusCode.Forbidden, forbidden.Message, null),
            AuthenticationException authentication => (HttpStatusCode.Unauthorized, authentication.Message, null),
            _ => (HttpStatusCode.InternalServerError, Messages.UnexpectedError, null),
        };

        var httpContext = context.HttpContext;
        var userId = httpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var correlationId = httpContext.TraceIdentifier;
        var requestPath = httpContext.Request.Path;

        if (context.Exception is EAutoskolaException)
        {
            _logger.LogWarning(context.Exception,
                "Handled application exception. Path: {RequestPath}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                requestPath, userId, correlationId);
        }
        else
        {
            _logger.LogError(context.Exception,
                "Unhandled exception. Path: {RequestPath}, UserId: {UserId}, CorrelationId: {CorrelationId}",
                requestPath, userId, correlationId);
        }

        httpContext.Response.StatusCode = (int)statusCode;

        object body = _environment.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError
            ? new { message, errors, detail = context.Exception.ToString() }
            : new { message, errors };

        context.Result = new JsonResult(body);
        context.ExceptionHandled = true;
    }
}
