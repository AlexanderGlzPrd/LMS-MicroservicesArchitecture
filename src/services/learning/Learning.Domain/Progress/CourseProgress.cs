using Learning.Domain.Abstractions;
using Learning.Domain.Progress.Events;
using Learning.Domain.Progress.Exceptions;
namespace Learning.Domain.Progress;

public sealed class CourseProgress
{
    private readonly List<CompletedLesson> _completedLessons = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    private CourseProgress()
    {
    }

    public static CourseProgress Start(StudentId studentId, CourseId courseId, DateTimeOffset startedAt)
    {
        EnsureNotEmpty(studentId.Value, nameof(studentId));
        EnsureNotEmpty(courseId.Value, nameof(courseId));

        var progress = new CourseProgress
        {
            StudentId = studentId,
            CourseId = courseId,
            Status = CourseProgressStatus.InProgress,
            StartedAt = startedAt,
        };

        progress._domainEvents.Add(new CourseProgressStarted(startedAt));

        return progress;
    }

    public bool MarkLessonCompleted(
        LessonId lessonId,
        IReadOnlySet<LessonId> publishedLessonIds,
        DateTimeOffset now)
    {
        EnsureNotEmpty(lessonId.Value, nameof(lessonId));

        if (!publishedLessonIds.Contains(lessonId))
        {
            throw new LessonNotInPublishedContentException(lessonId);
        }

        if (IsCompleted(lessonId))
        {
            return false;
        }

        _completedLessons.Add(CompletedLesson.Create(lessonId, now));
        _domainEvents.Add(new LessonCompleted(lessonId.Value, now, publishedLessonIds.Count));

        if (Status == CourseProgressStatus.InProgress && MeetsCompletionCriteria(publishedLessonIds))
        {
            Seal(now, publishedLessonIds.Count);
        }

        return true;
    }

    public bool ConfirmCompletion(IReadOnlySet<LessonId> publishedLessonIds, DateTimeOffset now)
    {
        if (Status == CourseProgressStatus.Completed)
        {
            return false;
        }

        if (!MeetsCompletionCriteria(publishedLessonIds))
        {
            throw new CompletionNotReadyException();
        }

        Seal(now, publishedLessonIds.Count);

        return true;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private bool MeetsCompletionCriteria(IReadOnlySet<LessonId> publishedLessonIds) =>
        publishedLessonIds.Count > 0 && publishedLessonIds.All(IsCompleted);

    private bool IsCompleted(LessonId lessonId) =>
        _completedLessons.Exists(completed => completed.LessonId == lessonId);

    private void Seal(DateTimeOffset now, int observedTotalLessonCount)
    {
        Status = CourseProgressStatus.Completed;
        CompletedAt = now;

        _domainEvents.Add(new CourseProgressCompleted(now, observedTotalLessonCount));
    }

    private static void EnsureNotEmpty(Guid value, string identityName)
    {
        if (value == Guid.Empty)
        {
            throw new InvalidLearningIdentityException(identityName);
        }
    }

    public StudentId StudentId { get; private set; }

    public CourseId CourseId { get; private set; }

    public CourseProgressStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyCollection<CompletedLesson> CompletedLessons => _completedLessons.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
}
