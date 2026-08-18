using Enrollments.Application.Abstractions;
using Enrollments.Domain.Enrollments;
using Microsoft.EntityFrameworkCore;
namespace Enrollments.Infrastructure.Persistence;
internal sealed class PurchaseGrantLedger(EnrollmentsDbContext context) : IPurchaseGrantLedger
{
    public async Task<PurchaseGrantEntry?> FindAsync(
        PurchaseId purchaseId,
        CancellationToken cancellationToken)
    {
        var row = await context.PurchaseGrants.FirstOrDefaultAsync(
            grant => grant.PurchaseId == purchaseId.Value, cancellationToken);

        return row is null ? null : ToEntry(row);
    }

    public void Add(PurchaseGrantEntry entry) =>
        context.PurchaseGrants.Add(new PurchaseGrant
        {
            PurchaseId = entry.PurchaseId.Value,
            StudentId = entry.StudentId.Value,
            CourseId = entry.CourseId.Value,
            Outcome = entry.Outcome.ToString(),
            Origin = entry.Origin.ToString(),
            RejectionReason = entry.RejectionReason,
            InitialMessageId = entry.InitialMessageId,
            ProcessedAt = entry.ProcessedAt,
        });

    private static PurchaseGrantEntry ToEntry(PurchaseGrant row) => new()
    {
        PurchaseId = new PurchaseId(row.PurchaseId),
        StudentId = new StudentId(row.StudentId),
        CourseId = new CourseId(row.CourseId),
        Outcome = Enum.Parse<PurchaseGrantOutcome>(row.Outcome),
        Origin = Enum.Parse<PurchaseGrantOrigin>(row.Origin),
        RejectionReason = row.RejectionReason,
        InitialMessageId = row.InitialMessageId,
        ProcessedAt = row.ProcessedAt,
    };
}