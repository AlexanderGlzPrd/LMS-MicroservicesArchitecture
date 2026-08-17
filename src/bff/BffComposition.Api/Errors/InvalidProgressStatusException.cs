namespace BffComposition.Api.Errors;
public sealed class InvalidProgressStatusException : Exception
{
    public InvalidProgressStatusException()
        : base("El filtro 'status' solo admite los valores 'InProgress' y 'Completed'.")
    {
    }
}
