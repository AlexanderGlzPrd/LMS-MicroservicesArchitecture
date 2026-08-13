using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enrollments;
namespace Enrollments.Api.Actor;
internal sealed class HttpCurrentActor(IHttpContextAccessor httpContextAccessor) : ICurrentActor
{
    public const string HeaderName = "X-Student-Id";

    public StudentId StudentId
    {
        get
        {
            var header = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString();
            return Guid.TryParse(header, out var studentId) && studentId != Guid.Empty
                ? new StudentId(studentId)
                : throw new MissingStudentHeaderException();
        }
    }
}
