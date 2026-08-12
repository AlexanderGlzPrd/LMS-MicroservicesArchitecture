using CourseAuthoring.Api.Actor;
using CourseAuthoring.Domain.Courses.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
namespace CourseAuthoring.Api.Errors;

/// <summary>
/// Traduce cualquier excepcion no controlada a <c>application/problem+json</c>.
/// </summary>
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
            InvalidCourseTitleException => (
                StatusCodes.Status400BadRequest,
                "Titulo de curso invalido",
                exception.Message),

            InvalidLessonTitleException => (
                StatusCodes.Status400BadRequest,
                "Titulo de leccion invalido",
                exception.Message),

            InvalidLessonDescriptionException => (
                StatusCodes.Status400BadRequest,
                "Descripcion de leccion invalida",
                exception.Message),

            InvalidVideoUrlException => (
                StatusCodes.Status400BadRequest,
                "URL de video invalida",
                exception.Message),

            MissingInstructorHeaderException => (
                StatusCodes.Status400BadRequest,
                "Instructor no identificado",
                exception.Message),

            CourseOwnershipException => (
                StatusCodes.Status403Forbidden,
                "El curso pertenece a otro instructor",
                exception.Message),

            LessonNotFoundException => (
                StatusCodes.Status404NotFound,
                "Leccion no encontrada",
                exception.Message),

            InvalidCourseStateException => (
                StatusCodes.Status409Conflict,
                "Estado del curso incompatible con la operacion",
                exception.Message),

            CourseHasNoLessonsException => (
                StatusCodes.Status422UnprocessableEntity,
                "El curso no tiene lecciones",
                exception.Message),

            InvalidLessonOrderException => (
                StatusCodes.Status422UnprocessableEntity,
                "Orden de lecciones invalido",
                exception.Message),

            // El detalle de un fallo no previsto no sale al cliente: queda en el log.
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
