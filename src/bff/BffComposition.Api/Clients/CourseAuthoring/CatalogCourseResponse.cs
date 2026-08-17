namespace BffComposition.Api.Clients.CourseAuthoring;

/// <summary>
/// Forma en que responde Course Authoring hoy. No sale de Clients/.
/// El detalle de cada leccion se descarta: al enriquecimiento solo le importa cuantas hay.
/// </summary>
internal sealed class CatalogCourseResponse
{
    public Guid? Id { get; init; }

    public string? Title { get; init; }

    public IReadOnlyList<CatalogLessonResponse>? Lessons { get; init; }
}

internal sealed class CatalogLessonResponse
{
    public Guid? Id { get; init; }
}
