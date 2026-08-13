using Enrollments.Domain.Enrollments;
namespace Enrollments.Application.Abstractions;
public interface ICurrentActor
{
    StudentId StudentId { get; }
}
