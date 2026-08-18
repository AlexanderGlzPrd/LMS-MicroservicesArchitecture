using PaidEnrollment.Application.Abstractions;
namespace PaidEnrollment.Api.Actor;
internal sealed class HttpCurrentOperator(IHttpContextAccessor httpContextAccessor)
    : ICurrentOperator
{
    public const string HeaderName = "X-Operator-Id";

    public Guid OperatorId
    {
        get
        {
            var header = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString();

            return Guid.TryParse(header, out var operatorId) && operatorId != Guid.Empty
                ? operatorId
                : throw new MissingOperatorHeaderException();
        }
    }
}