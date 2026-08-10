namespace CourseAuthoring.Domain.Courses;

/// <summary>
/// Estados que el servicio sabe producir hoy. <c>Published</c> entra en la SPEC 02,
/// junto con la accion <c>Publish</c> que lo produce.
/// </summary>
public enum CourseStatus
{
    Draft = 1,
}
