using System.Security.Claims;

using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Domain.Courses;
namespace CourseAuthoring.Api.Actor;
internal sealed class HttpCurrentActor(IHttpContextAccessor httpContextAccessor) : ICurrentActor
{
    public InstructorId InstructorId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            return Guid.TryParse(subject, out var instructorId)
                ? new InstructorId(instructorId)
                : throw new InvalidActorClaimException();
        }
    }
}
