using Certification.Application.Abstractions;
namespace Certification.Api.Actor;

internal sealed class HttpCurrentActor(IHttpContextAccessor httpContextAccessor) : ICurrentActor
{
    public const string HeaderName = "X-Student-Id";

    public Guid StudentId
    {
        get
        {
            var header = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString();

            return Guid.TryParse(header, out var studentId) && studentId != Guid.Empty
                ? studentId
                : throw new MissingStudentHeaderException();
        }
    }
}
