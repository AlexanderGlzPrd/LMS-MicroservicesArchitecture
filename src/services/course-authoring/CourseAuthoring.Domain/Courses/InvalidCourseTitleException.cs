namespace CourseAuthoring.Domain.Courses;

public sealed class InvalidCourseTitleException : Exception
{
    public InvalidCourseTitleException()
        : base("El titulo del curso no puede estar vacio.")
    {
    }
}
