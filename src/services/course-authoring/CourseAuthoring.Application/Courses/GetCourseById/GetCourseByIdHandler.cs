using CourseAuthoring.Application.Abstractions;

namespace CourseAuthoring.Application.Courses.GetCourseById;

public sealed class GetCourseByIdHandler(ICourseRepository courses)
{
    /// <summary>
    /// Devuelve <c>null</c> si el curso no existe.
    /// </summary>
    public async Task<CourseView?> HandleAsync(
        GetCourseByIdQuery query,
        CancellationToken cancellationToken)
    {
        var course = await courses.GetByIdAsync(query.CourseId, cancellationToken);

        return course is null ? null : CourseView.From(course);
    }
}
