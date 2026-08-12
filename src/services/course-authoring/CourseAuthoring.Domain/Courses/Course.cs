using CourseAuthoring.Domain.Courses.Exceptions;

namespace CourseAuthoring.Domain.Courses;

public sealed class Course
{
    private readonly List<Lesson> _workingLessons = [];

    private Course()
    {
    }

    /// <exception cref="InvalidCourseTitleException">Si el titulo esta vacio o son solo espacios.</exception>
    public static Course Create(
        CourseId id,
        InstructorId instructorId,
        string title,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidCourseTitleException();
        }

        return new Course
        {
            Id = id,
            InstructorId = instructorId,
            Title = title,
            Status = CourseStatus.Draft,
            CreatedAt = createdAt,
        };
    }

    /// <exception cref="CourseOwnershipException">Si el actor no es el propietario.</exception>
    public LessonId AddLesson(
        InstructorId actor,
        LessonId lessonId,
        string title,
        string description,
        string videoUrl)
    {
        EnsureOwnership(actor);

        var lesson = Lesson.Create(lessonId, Id, title, description, videoUrl, _workingLessons.Count + 1);
        _workingLessons.Add(lesson);

        return lessonId;
    }

    /// <exception cref="CourseOwnershipException">Si el actor no es el propietario.</exception>
    /// <exception cref="LessonNotFoundException">Si la leccion no pertenece a este curso.</exception>
    public void UpdateLesson(
        InstructorId actor,
        LessonId lessonId,
        string title,
        string description,
        string videoUrl)
    {
        EnsureOwnership(actor);

        RequireWorkingLesson(lessonId).Update(title, description, videoUrl);
    }


    public void RemoveLesson(InstructorId actor, LessonId lessonId)
    {
        EnsureOwnership(actor);

        var lesson = RequireWorkingLesson(lessonId);
        _workingLessons.Remove(lesson);

        Recompact();
    }

    /// <exception cref="InvalidLessonOrderException">Si la lista no es una permutacion exacta.</exception>
    public void ReorderLessons(InstructorId actor, IReadOnlyList<LessonId> orderedLessonIds)
    {
        EnsureOwnership(actor);
        ArgumentNullException.ThrowIfNull(orderedLessonIds);

        if (!IsExactPermutation(orderedLessonIds))
        {
            throw new InvalidLessonOrderException();
        }

        for (var index = 0; index < orderedLessonIds.Count; index++)
        {
            RequireWorkingLesson(orderedLessonIds[index]).MoveTo(index + 1);
        }
    }

    /// <exception cref="InvalidCourseTitleException">Si el titulo esta vacio o son solo espacios.</exception>
    public void Rename(InstructorId actor, string title)
    {
        EnsureOwnership(actor);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidCourseTitleException();
        }

        Title = title;
    }

    private void EnsureOwnership(InstructorId actor)
    {
        if (actor != InstructorId)
        {
            throw new CourseOwnershipException(Id, actor);
        }
    }

    private Lesson RequireWorkingLesson(LessonId lessonId)
        => _workingLessons.SingleOrDefault(lesson => lesson.Id == lessonId)
           ?? throw new LessonNotFoundException(Id, lessonId);

    private bool IsExactPermutation(IReadOnlyList<LessonId> orderedLessonIds)
    {
        if (orderedLessonIds.Count != _workingLessons.Count)
        {
            return false;
        }

        var received = new HashSet<LessonId>(orderedLessonIds);

        return received.Count == orderedLessonIds.Count
               && received.SetEquals(_workingLessons.Select(lesson => lesson.Id));
    }

    private void Recompact()
    {
        var position = 1;

        foreach (var lesson in _workingLessons.OrderBy(lesson => lesson.Position).ToList())
        {
            lesson.MoveTo(position++);
        }
    }

    public CourseId Id { get; private set; }

    public InstructorId InstructorId { get; private set; }

    public string Title { get; private set; } = null!;

    public CourseStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<Lesson> WorkingLessons => [.. _workingLessons.OrderBy(lesson => lesson.Position)];
}
