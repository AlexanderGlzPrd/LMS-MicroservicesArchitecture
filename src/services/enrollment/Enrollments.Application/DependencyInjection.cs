using Enrollments.Application.Enrollments.EnrollStudent;
using Enrollments.Application.Enrollments.GetEnrollmentAccess;
using Enrollments.Application.Enrollments.GetStudentEnrollment;
using Enrollments.Application.Enrollments.GrantEnrollmentForCapturedPayment;
using Enrollments.Application.Enrollments.ListStudentEnrollments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace Enrollments.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<EnrollStudentHandler>();
        services.AddScoped<GrantEnrollmentForCapturedPaymentHandler>();
        services.AddScoped<ListStudentEnrollmentsHandler>();
        services.AddScoped<GetStudentEnrollmentHandler>();
        services.AddScoped<GetEnrollmentAccessHandler>();

        return services;
    }
}
