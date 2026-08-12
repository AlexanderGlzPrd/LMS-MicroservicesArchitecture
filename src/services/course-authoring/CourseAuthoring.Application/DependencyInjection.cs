using CourseAuthoring.Application.Courses.AddLesson;
using CourseAuthoring.Application.Courses.CreateCourse;
using CourseAuthoring.Application.Courses.GetCourseById;
using CourseAuthoring.Application.Courses.RemoveLesson;
using CourseAuthoring.Application.Courses.RenameCourse;
using CourseAuthoring.Application.Courses.ReorderLessons;
using CourseAuthoring.Application.Courses.UpdateLesson;
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

        services.AddScoped<AddLessonHandler>();
        services.AddScoped<UpdateLessonHandler>();
        services.AddScoped<RemoveLessonHandler>();
        services.AddScoped<ReorderLessonsHandler>();
        services.AddScoped<RenameCourseHandler>();

        return services;
    }
}
