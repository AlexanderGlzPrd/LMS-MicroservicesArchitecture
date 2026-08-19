using System.Security.Claims;

using PaidEnrollment.Application.Abstractions;
namespace PaidEnrollment.Api.Actor;
internal sealed class HttpCurrentOperator(IHttpContextAccessor httpContextAccessor)
    : ICurrentOperator
{
    public Guid OperatorId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            return Guid.TryParse(subject, out var operatorId) && operatorId != Guid.Empty
                ? operatorId
                : throw new InvalidActorClaimException();
        }
    }
}
