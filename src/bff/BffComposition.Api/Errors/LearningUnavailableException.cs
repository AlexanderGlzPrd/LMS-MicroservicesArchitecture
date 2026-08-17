namespace BffComposition.Api.Errors;
public sealed class LearningUnavailableException : Exception
{
    public LearningUnavailableException()
        : base("No se pudo obtener el progreso del estudiante desde Learning.")
    {
    }
}