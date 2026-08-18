namespace PaidEnrollment.Api.Actor;
public sealed class MissingOperatorHeaderException : Exception
{
    public MissingOperatorHeaderException()
        : base("La cabecera X-Operator-Id es obligatoria y debe contener un GUID valido y no nulo.")
    {
    }
}