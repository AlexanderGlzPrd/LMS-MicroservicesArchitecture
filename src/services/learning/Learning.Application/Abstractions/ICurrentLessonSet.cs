using Learning.Domain.Progress;

namespace Learning.Application.Abstractions;

public interface ICurrentLessonSet
{
    Task<CurrentLessonSet> GetAsync(CourseId courseId, CancellationToken cancellationToken);
}
