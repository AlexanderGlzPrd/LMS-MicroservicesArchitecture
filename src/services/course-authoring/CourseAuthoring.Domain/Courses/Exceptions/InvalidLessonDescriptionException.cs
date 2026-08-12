using CourseAuthoring.Domain.Abstractions;

namespace CourseAuthoring.Domain.Courses.Exceptions;

public sealed class InvalidLessonDescriptionException()
    : DomainException("La descripcion de la leccion no puede estar vacia.");
