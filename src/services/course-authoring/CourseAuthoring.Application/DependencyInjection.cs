using CourseAuthoring.Application.Courses.CreateCourse;
using CourseAuthoring.Application.Courses.GetCourseById;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CourseAuthoring.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<CreateCourseHandler>();
        services.AddScoped<GetCourseByIdHandler>();

        return services;
    }
}
