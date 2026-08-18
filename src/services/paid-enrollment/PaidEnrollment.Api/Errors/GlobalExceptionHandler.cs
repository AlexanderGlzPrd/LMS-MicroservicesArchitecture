using Microsoft.AspNetCore.Diagnostics;
using PaidEnrollment.Api.Actor;
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
            MissingStudentHeaderException => (
                StatusCodes.Status400BadRequest,
                "Estudiante no identificado",
                exception.Message),

            MissingOperatorHeaderException => (
                StatusCodes.Status400BadRequest,
                "Operador no identificado",
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