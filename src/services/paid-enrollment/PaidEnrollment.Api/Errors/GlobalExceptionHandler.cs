using Microsoft.AspNetCore.Diagnostics;
using PaidEnrollment.Api.Actor;
using PaidEnrollment.Application.Abstractions.Exceptions;
namespace PaidEnrollment.Api.Errors;
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
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

            PurchaseNotFoundException => (
                StatusCodes.Status404NotFound,
                "Compra no encontrada",
                exception.Message),

            PurchaseClosedForCourseException => (
                StatusCodes.Status409Conflict,
                "Compra cerrada para ese curso",
                exception.Message),

            PurchaseNotUnderManualReviewException => (
                StatusCodes.Status409Conflict,
                "Compra fuera de revision manual",
                exception.Message),

            PurchaseAmountNotConfiguredException => (
                StatusCodes.Status422UnprocessableEntity,
                "Curso sin importe configurado",
                exception.Message),

            ManualResolutionNotApplicableException => (
                StatusCodes.Status422UnprocessableEntity,
                "Resolucion no aplicable",
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