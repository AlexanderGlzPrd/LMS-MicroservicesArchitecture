using Learning.Domain.Progress;

namespace Learning.Application.Abstractions;

public interface ICourseProgressRepository
{
    Task<CourseProgress?> FindAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CourseProgress>> ListByStudentAsync(
        StudentId studentId,
        CourseProgressStatus? status,
        CancellationToken cancellationToken);

    void Add(CourseProgress progress);
}
