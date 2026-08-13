namespace Learning.Application.Abstractions.Exceptions;

public sealed class ConcurrentCourseProgressException(Exception innerException)
    : Exception(
        "Otra peticion simultanea escribio antes sobre el mismo progreso o la misma leccion completada.",
        innerException);
