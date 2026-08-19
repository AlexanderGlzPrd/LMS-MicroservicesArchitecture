using System.Security.Claims;

using PaidEnrollment.Application.Abstractions;
namespace PaidEnrollment.Api.Actor;
internal sealed class HttpCurrentActor(IHttpContextAccessor httpContextAccessor) : ICurrentActor
{
    public Guid StudentId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            return Guid.TryParse(subject, out var studentId) && studentId != Guid.Empty
                ? studentId
                : throw new InvalidActorClaimException();
        }
    }
}
