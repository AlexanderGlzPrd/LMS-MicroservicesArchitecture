using Learning.Application.Progress.ConfirmCompletion;
using Learning.Application.Progress.MarkLessonCompleted;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Learning.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<MarkLessonCompletedHandler>();
        services.AddScoped<ConfirmCompletionHandler>();

        return services;
    }
}
