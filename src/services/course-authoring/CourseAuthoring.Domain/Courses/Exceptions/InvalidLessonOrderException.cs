using CourseAuthoring.Domain.Abstractions;

namespace CourseAuthoring.Domain.Courses.Exceptions;

public sealed class InvalidLessonOrderException()
    : DomainException(
        "La lista de reordenamiento debe ser una permutacion exacta de las lecciones del curso: "
        + "sin duplicados, sin ausencias y sin identificadores ajenos.");
