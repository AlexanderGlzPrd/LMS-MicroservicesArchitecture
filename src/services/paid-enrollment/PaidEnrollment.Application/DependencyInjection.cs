using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PaidEnrollment.Application.Purchases.GetPurchase;
using PaidEnrollment.Application.Purchases.ResolveManualReview;
using PaidEnrollment.Application.Purchases.StartPurchase;
using PaidEnrollment.Application.Purchases.Workflow;
namespace PaidEnrollment.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<StartPurchaseHandler>();
        services.AddScoped<GetPurchaseHandler>();

        services.AddScoped<ResolveManualReviewHandler>();

        services.AddScoped<PurchaseAdvancer>();
        services.AddScoped<PurchaseReconciliation>();
        services.AddScoped<PurchaseWorkflow>();

        return services;
    }
}
