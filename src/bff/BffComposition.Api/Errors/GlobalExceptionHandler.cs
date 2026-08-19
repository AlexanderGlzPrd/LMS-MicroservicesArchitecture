using System.Globalization;
using BffComposition.Api.Clients.Learning;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
namespace BffComposition.Api.Errors;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IOptions<LearningOptions> learningOptions,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Peticion cancelada por el llamante en {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            return true;
        }

        var (statusCode, title, detail) = exception switch
        {
            InvalidProgressStatusException => (
                StatusCodes.Status400BadRequest,
                "Filtro de estado no admitido",
                exception.Message),

            LearningUnavailableException => (
                StatusCodes.Status503ServiceUnavailable,
                "Dependencia esencial no disponible",
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
            httpContext.Response.Headers.RetryAfter = learningOptions.Value
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
