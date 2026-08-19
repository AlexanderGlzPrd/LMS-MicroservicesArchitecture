using PaidEnrollment.Domain.Purchases;
namespace PaidEnrollment.Application.Abstractions;
public interface ISagaMetrics
{
    void RecordTransition(PurchaseStatus from, PurchaseStatus to, string result);

    void RecordCompensation(string operation, string result);
}