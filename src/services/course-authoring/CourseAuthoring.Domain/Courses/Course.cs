using CourseAuthoring.Domain.Abstractions;
using CourseAuthoring.Domain.Courses.Events;
using CourseAuthoring.Domain.Courses.Exceptions;

namespace CourseAuthoring.Domain.Courses;

public sealed class Course
{
    private readonly List<Lesson> _workingLessons = [];
    private readonly List<PublishedLesson> _publishedLessons = [];
    private readonly List<IDomainEvent> _domainEvents = [];

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

    /// <exception cref="CourseOwnershipException">Si el actor no es el propietario.</exception>
    /// <exception cref="InvalidCourseStateException">Si el curso no esta en Draft.</exception>
    /// <exception cref="CourseHasNoLessonsException">Si no hay lecciones de trabajo.</exception>
    public void Publish(InstructorId actor, DateTimeOffset publishedAt)
    {
        EnsureOwnership(actor);
        EnsureStatus(CourseStatus.Draft);
        EnsureHasWorkingLessons();

        ReplacePublishedContent();

        Status = CourseStatus.Published;
        PublishedAt = publishedAt;
        PublishedContentUpdatedAt = publishedAt;

        _domainEvents.Add(new CoursePublished(Id, InstructorId, publishedAt));
    }

    // Devuelve false cuando no hay cambios estructurales: el no-op no toca fechas ni registra evento.
    /// <exception cref="CourseOwnershipException">Si el actor no es el propietario.</exception>
    /// <exception cref="InvalidCourseStateException">Si el curso no esta publicado.</exception>
    /// <exception cref="CourseHasNoLessonsException">Si no hay lecciones de trabajo.</exception>
    public bool Republish(InstructorId actor, DateTimeOffset republishedAt)
    {
        EnsureOwnership(actor);
        EnsureStatus(CourseStatus.Published);
        EnsureHasWorkingLessons();

        if (!HasStructuralChanges())
        {
            return false;
        }

        ReplacePublishedContent();
        PublishedContentUpdatedAt = republishedAt;

        _domainEvents.Add(new PublishedContentModified(Id, republishedAt));

        return true;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void EnsureOwnership(InstructorId actor)
    {
        if (actor != InstructorId)
        {
            throw new CourseOwnershipException(Id, actor);
        }
    }

    private void EnsureStatus(CourseStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidCourseStateException(Id, Status, expected);
        }
    }

    private void EnsureHasWorkingLessons()
    {
        if (_workingLessons.Count == 0)
        {
            throw new CourseHasNoLessonsException(Id);
        }
    }

    // "Reemplazar el snapshot" es semantica de dominio. Como se persiste ese resultado
    // (reconciliando por LessonId) es cosa de Infrastructure.
    private void ReplacePublishedContent()
    {
        _publishedLessons.Clear();
        _publishedLessons.AddRange(WorkingLessons.Select(PublishedLesson.From));

        PublishedTitle = Title;
    }

    // La verdad esta en los datos: no hay bandera de cambios pendientes que pueda desincronizarse.
    private bool HasStructuralChanges()
    {
        if (!string.Equals(Title, PublishedTitle, StringComparison.Ordinal))
        {
            return true;
        }

        var working = WorkingLessons;
        var published = PublishedLessons;

        if (working.Count != published.Count)
        {
            return true;
        }

        for (var index = 0; index < working.Count; index++)
        {
            if (!IsSameContent(working[index], published[index]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSameContent(Lesson working, PublishedLesson published)
        => working.Id == published.Id
           && working.Position == published.Position
           && string.Equals(working.Title, published.Title, StringComparison.Ordinal)
           && string.Equals(working.Description, published.Description, StringComparison.Ordinal)
           && string.Equals(working.VideoUrl, published.VideoUrl, StringComparison.Ordinal);

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

    public string? PublishedTitle { get; private set; }

    // Primera publicacion: no cambia al republicar.
    public DateTimeOffset? PublishedAt { get; private set; }

    // Ultima republicacion con cambios.
    public DateTimeOffset? PublishedContentUpdatedAt { get; private set; }

    public IReadOnlyList<Lesson> WorkingLessons => [.. _workingLessons.OrderBy(lesson => lesson.Position)];

    public IReadOnlyList<PublishedLesson> PublishedLessons =>
        [.. _publishedLessons.OrderBy(lesson => lesson.Position)];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
}
