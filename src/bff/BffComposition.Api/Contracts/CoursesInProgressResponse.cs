namespace BffComposition.Api.Contracts;

public sealed record CoursesInProgressResponse(
    IReadOnlyList<CourseInProgressItemResponse> Items,
    bool IsPartial,
    IReadOnlyList<CompositionWarningResponse> Warnings);
