using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Abstractions;
public interface ICourseAvailability
{
    Task<CourseAvailability> CheckAsync(CourseId courseId, CancellationToken cancellationToken);
}
