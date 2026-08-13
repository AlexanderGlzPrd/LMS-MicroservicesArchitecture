using Learning.Domain.Progress;

namespace Learning.Application.Progress.MarkLessonCompleted;

public sealed record MarkLessonCompletedCommand(CourseId CourseId, LessonId LessonId);
