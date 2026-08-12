using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Domain.Courses;

namespace CourseAuthoring.Application.Tests.Fakes;

internal sealed class InMemoryCourseRepository : ICourseRepository
{
    private readonly Dictionary<CourseId, Course> courses = [];

    public Task<Course?> GetByIdAsync(CourseId id, CancellationToken cancellationToken) =>
        Task.FromResult(courses.GetValueOrDefault(id));

    public Task<IReadOnlyList<Course>> ListByInstructorAsync(
        InstructorId instructorId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Course>>(
        [
            .. courses.Values
                .Where(course => course.InstructorId == instructorId)
                .OrderByDescending(course => course.CreatedAt)
                .ThenBy(course => course.Id.Value)
        ]);

    public void Add(Course course) => courses[course.Id] = course;
}
