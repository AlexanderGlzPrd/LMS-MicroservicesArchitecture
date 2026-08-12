using CourseAuthoring.Application.Abstractions;

namespace CourseAuthoring.Application.Courses.ListInstructorCourses;

public sealed class ListInstructorCoursesHandler(
    ICourseRepository courses,
    ICurrentActor currentActor)
{
    public async Task<IReadOnlyList<CourseSummaryView>> HandleAsync(
        ListInstructorCoursesQuery query,
        CancellationToken cancellationToken)
    {
        var instructorCourses = await courses.ListByInstructorAsync(
            currentActor.InstructorId,
            cancellationToken);

        return [.. instructorCourses.Select(CourseSummaryView.From)];
    }
}
