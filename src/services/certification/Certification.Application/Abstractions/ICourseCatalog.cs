namespace Certification.Application.Abstractions;
public interface ICourseCatalog
{
    Task<CourseTitleLookup> GetTitleAsync(Guid courseId, CancellationToken cancellationToken);
}