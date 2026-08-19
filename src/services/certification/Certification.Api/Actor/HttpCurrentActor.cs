using System.Security.Claims;

using Certification.Application.Abstractions;
namespace Certification.Api.Actor;

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
