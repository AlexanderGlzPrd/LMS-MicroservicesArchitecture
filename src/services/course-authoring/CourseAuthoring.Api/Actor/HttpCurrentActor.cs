using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Domain.Courses;
namespace CourseAuthoring.Api.Actor;

internal sealed class HttpCurrentActor(IHttpContextAccessor httpContextAccessor) : ICurrentActor
{
    public const string HeaderName = "X-Instructor-Id";

    public InstructorId InstructorId
    {
        get
        {
            var header = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString();

            return Guid.TryParse(header, out var instructorId)
                ? new InstructorId(instructorId)
                : throw new MissingInstructorHeaderException();
        }
    }
}
