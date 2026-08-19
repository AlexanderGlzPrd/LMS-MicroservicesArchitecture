using System.Globalization;
using Enrollments.Api.Actor;
using Enrollments.Application.Abstractions.Exceptions;
using Enrollments.Infrastructure.Acl;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
namespace Enrollments.Api.Errors;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IOptions<CourseAuthoringOptions> courseAuthoringOptions,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            InvalidActorClaimException => (
                StatusCodes.Status401Unauthorized,
                "Actor no identificado",
                exception.Message),

            CourseNotAvailableException => (
                StatusCodes.Status422UnprocessableEntity,
                "Curso no matriculable",
                exception.Message),

            CourseAvailabilityUnknownException => (
                StatusCodes.Status503ServiceUnavailable,
                "Precondicion no verificable",
                exception.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Error interno del servidor",
                "Se ha producido un error inesperado al procesar la peticion."),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Excepcion no controlada en {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Peticion rechazada en {Method} {Path}: {Reason}",
                httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        if (statusCode == StatusCodes.Status503ServiceUnavailable)
        {
            httpContext.Response.Headers.RetryAfter = courseAuthoringOptions.Value
                .RetryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
            },
        });
    }
}
