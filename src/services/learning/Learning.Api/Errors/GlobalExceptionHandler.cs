using System.Globalization;

using Learning.Api.Actor;
using Learning.Application.Abstractions.Exceptions;
using Learning.Domain.Progress.Exceptions;
using Learning.Infrastructure.Acl;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
namespace Learning.Api.Errors;

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
            MissingStudentHeaderException => (
                StatusCodes.Status400BadRequest,
                "Estudiante no identificado",
                exception.Message),

            CourseProgressNotFoundException => (
                StatusCodes.Status404NotFound,
                "Progreso no encontrado",
                exception.Message),

            CourseNotAvailableException => (
                StatusCodes.Status422UnprocessableEntity,
                "Curso no disponible",
                exception.Message),

            LessonNotInPublishedContentException => (
                StatusCodes.Status422UnprocessableEntity,
                "Leccion fuera del contenido publicado",
                exception.Message),

            CompletionNotReadyException => (
                StatusCodes.Status422UnprocessableEntity,
                "Finalizacion no disponible",
                exception.Message),

            CurrentLessonSetUnknownException => (
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
