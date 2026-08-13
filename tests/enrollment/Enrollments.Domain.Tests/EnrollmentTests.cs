using System.Reflection;

using Enrollments.Domain.Enrollments;
using Enrollments.Domain.Enrollments.Exceptions;

namespace Enrollments.Domain.Tests;

public sealed class EnrollmentTests
{
    private static readonly EnrollmentId Id = new(Guid.CreateVersion7());
    private static readonly StudentId Student = new(Guid.CreateVersion7());
    private static readonly CourseId Course = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset EnrolledAt = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GrantFree_DejaLaMatriculaComoGratuita()
    {
        var enrollment = Enrollment.GrantFree(Id, Student, Course, EnrolledAt);

        Assert.Equal(EnrollmentType.Free, enrollment.Type);
    }

    [Fact]
    public void GrantFree_ConservaLosDatosRecibidos()
    {
        var enrollment = Enrollment.GrantFree(Id, Student, Course, EnrolledAt);

        Assert.Equal(Id, enrollment.Id);
        Assert.Equal(Student, enrollment.StudentId);
        Assert.Equal(Course, enrollment.CourseId);
        Assert.Equal(EnrolledAt, enrollment.EnrolledAt);
    }

    [Fact]
    public void GrantFree_ConStudentIdVacio_LanzaExcepcionDeDominio()
    {
        Assert.Throws<InvalidEnrollmentIdentityException>(
            () => Enrollment.GrantFree(Id, new StudentId(Guid.Empty), Course, EnrolledAt));
    }

    [Fact]
    public void GrantFree_ConCourseIdVacio_LanzaExcepcionDeDominio()
    {
        Assert.Throws<InvalidEnrollmentIdentityException>(
            () => Enrollment.GrantFree(Id, Student, new CourseId(Guid.Empty), EnrolledAt));
    }

    [Fact]
    public void Enrollment_NoExponeNingunSetterPublicoDeLaClaveNatural()
    {
        var setters = typeof(Enrollment)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.SetMethod)
            .Where(setter => setter is not null && setter.IsPublic);

        Assert.Empty(setters);
    }

    [Fact]
    public void EnrollmentType_DeclaraUnUnicoValor()
    {
        Assert.Equal([EnrollmentType.Free], Enum.GetValues<EnrollmentType>());
    }
}
