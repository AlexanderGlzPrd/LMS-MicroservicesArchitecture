using Learning.Application.Progress.ConfirmCompletion;
using Learning.Application.Progress.GetCourseProgress;
using Learning.Application.Progress.ListStudentProgress;
using Learning.Application.Progress.MarkLessonCompleted;
using Learning.Application.Progress.RecognizeGrantedAccess;

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
        services.AddScoped<RecognizeGrantedAccessHandler>();

        services.AddScoped<ListStudentProgressHandler>();
        services.AddScoped<GetCourseProgressHandler>();

        return services;
    }
}
