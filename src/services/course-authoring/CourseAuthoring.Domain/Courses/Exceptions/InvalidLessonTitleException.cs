using CourseAuthoring.Domain.Abstractions;

namespace CourseAuthoring.Domain.Courses.Exceptions;

public sealed class InvalidLessonTitleException()
    : DomainException("El titulo de la leccion no puede estar vacio.");
