namespace CourseAuthoring.Api.Actor;
public sealed class InvalidActorClaimException : Exception
{
    public InvalidActorClaimException()
        : base("El token no contiene un claim 'sub' con un GUID valido.")
    {
    }
}
