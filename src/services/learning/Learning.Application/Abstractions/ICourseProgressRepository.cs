using Learning.Domain.Progress;

namespace Learning.Application.Abstractions;

public interface ICourseProgressRepository
{
    Task<CourseProgress?> FindAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken);

    void Add(CourseProgress progress);
}
