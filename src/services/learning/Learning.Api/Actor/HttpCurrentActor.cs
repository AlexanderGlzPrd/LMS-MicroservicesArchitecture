using System.Security.Claims;
using Learning.Application.Abstractions;
using Learning.Domain.Progress;
namespace Learning.Api.Actor;

internal sealed class HttpCurrentActor(IHttpContextAccessor httpContextAccessor) : ICurrentActor
{
    public StudentId StudentId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            return Guid.TryParse(subject, out var studentId) && studentId != Guid.Empty
                ? new StudentId(studentId)
                : throw new InvalidActorClaimException();
        }
    }
}
