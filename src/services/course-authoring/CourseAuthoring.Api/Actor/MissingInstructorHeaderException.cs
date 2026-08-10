namespace CourseAuthoring.Api.Actor;
public sealed class MissingInstructorHeaderException : Exception
{
    public MissingInstructorHeaderException()
        : base("La cabecera X-Instructor-Id es obligatoria y debe contener un GUID valido.")
    {
    }
}
