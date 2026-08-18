using MassTransit;
using PaidEnrollment.Contracts.V1;
using PaymentProviderSim.Worker.Payments;
namespace PaymentProviderSim.Worker.Messaging;
internal sealed class PaymentCommandConsumer(PaymentService payments) :
    IConsumer<AuthorizePayment>,
    IConsumer<CapturePayment>,
    IConsumer<VoidAuthorization>,
    IConsumer<RefundPayment>,
    IConsumer<GetPaymentStatus>
{
    public async Task Consume(ConsumeContext<AuthorizePayment> context)
    {
        var command = context.Message;
        var messageId = EnsureValid(
            context.MessageId,
            nameof(AuthorizePayment),
            command.PurchaseId,
            command.PaymentId,
            command.OccurredAt);

        if (command.Amount <= 0)
        {
            throw new InvalidPaymentCommandMessageException(
                nameof(AuthorizePayment), "Amount no es positivo.");
        }

        if (string.IsNullOrWhiteSpace(command.Currency))
        {
            throw new InvalidPaymentCommandMessageException(
                nameof(AuthorizePayment), "Currency no tiene valor.");
        }

        await payments.AuthorizeAsync(
            messageId,
            typeof(AuthorizePayment).FullName!,
            command,
            context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<CapturePayment> context)
    {
        var command = context.Message;
        var messageId = EnsureValid(
            context.MessageId,
            nameof(CapturePayment),
            command.PurchaseId,
            command.PaymentId,
            command.OccurredAt);

        await payments.CaptureAsync(
            messageId,
            typeof(CapturePayment).FullName!,
            command,
            context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<VoidAuthorization> context)
    {
        var command = context.Message;
        var messageId = EnsureValid(
            context.MessageId,
            nameof(VoidAuthorization),
            command.PurchaseId,
            command.PaymentId,
            command.OccurredAt);

        await payments.VoidAsync(
            messageId,
            typeof(VoidAuthorization).FullName!,
            command,
            context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<RefundPayment> context)
    {
        var command = context.Message;
        var messageId = EnsureValid(
            context.MessageId,
            nameof(RefundPayment),
            command.PurchaseId,
            command.PaymentId,
            command.OccurredAt);

        await payments.RefundAsync(
            messageId,
            typeof(RefundPayment).FullName!,
            command,
            context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<GetPaymentStatus> context)
    {
        var command = context.Message;
        var messageId = EnsureValid(
            context.MessageId,
            nameof(GetPaymentStatus),
            command.PurchaseId,
            command.PaymentId,
            command.OccurredAt);

        await payments.ReportStatusAsync(
            messageId,
            typeof(GetPaymentStatus).FullName!,
            command,
            context.CancellationToken);
    }

    private static Guid EnsureValid(
        Guid? messageId,
        string command,
        Guid purchaseId,
        Guid paymentId,
        DateTimeOffset occurredAt)
    {
        if (messageId is null)
        {
            throw new InvalidPaymentCommandMessageException(
                command, "el sobre no trae MessageId.");
        }

        if (purchaseId == Guid.Empty)
        {
            throw new InvalidPaymentCommandMessageException(command, "PurchaseId esta a ceros.");
        }

        if (paymentId == Guid.Empty)
        {
            throw new InvalidPaymentCommandMessageException(command, "PaymentId esta a ceros.");
        }

        if (occurredAt == default)
        {
            throw new InvalidPaymentCommandMessageException(command, "OccurredAt no tiene valor.");
        }

        return messageId.Value;
    }
}