namespace BffComposition.Api.Contracts;
public sealed record CompositionWarningResponse(
    Guid CourseId,
    string Code,
    string Message);