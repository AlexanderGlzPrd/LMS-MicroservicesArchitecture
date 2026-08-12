using CourseAuthoring.Domain.Abstractions;

namespace CourseAuthoring.Domain.Courses.Exceptions;

public sealed class InvalidCourseTitleException()
    : DomainException("El titulo del curso no puede estar vacio.");
