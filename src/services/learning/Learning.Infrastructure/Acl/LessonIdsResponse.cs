namespace Learning.Infrastructure.Acl;

// DTO interno del adaptador: no sale de Infrastructure/Acl. Los dos campos son anulables
// porque sobre un Guid no anulable un campo ausente y uno a ceros son indistinguibles.
internal sealed class LessonIdsResponse
{
    public Guid? CourseId { get; init; }

    public IReadOnlyList<Guid>? LessonIds { get; init; }
}
