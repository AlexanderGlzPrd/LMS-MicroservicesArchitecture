using Certification.Application.Certificates.AcceptCourseCompletion;
using Certification.Application.Certificates.GetCertificate;
using Certification.Application.Certificates.ListStudentCertificates;
using Certification.Application.Certificates.VerifyCertificate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace Certification.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<AcceptCourseCompletionHandler>();

        services.AddScoped<VerifyCertificateHandler>();
        services.AddScoped<GetCertificateHandler>();
        services.AddScoped<ListStudentCertificatesHandler>();

        return services;
    }
}
