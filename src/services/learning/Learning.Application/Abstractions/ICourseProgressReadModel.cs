using Learning.Application.Progress;
using Learning.Domain.Progress;
namespace Learning.Application.Abstractions;
public interface ICourseProgressReadModel
{
    Task<CourseProgressView?> FindAsync(
        StudentId studentId,
        CourseId courseId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CourseProgressView>> ListByStudentAsync(
        StudentId studentId,
        CourseProgressStatus? status,
        CancellationToken cancellationToken);
}