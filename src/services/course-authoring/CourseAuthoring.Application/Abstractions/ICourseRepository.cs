using CourseAuthoring.Domain.Courses;
namespace CourseAuthoring.Application.Abstractions;
public interface ICourseRepository
{
    Task<Course?> GetByIdAsync(CourseId id, CancellationToken cancellationToken);

    void Add(Course course);
}