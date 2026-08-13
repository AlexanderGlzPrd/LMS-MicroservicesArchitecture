namespace Enrollments.Api.Actor;
public sealed class MissingStudentHeaderException : Exception
{
    public MissingStudentHeaderException()
        : base("La cabecera X-Student-Id es obligatoria y debe contener un GUID valido y no nulo.")
    {
    }
}
