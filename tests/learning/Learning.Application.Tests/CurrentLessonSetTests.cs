using System.Reflection;
using Learning.Application.Abstractions;
using Learning.Domain.Progress;
namespace Learning.Application.Tests;

public sealed class CurrentLessonSetTests
{
    private static readonly LessonId Lesson = new(Guid.CreateVersion7());

    [Fact]
    public void Available_ConservaElConjuntoRecibido()
    {
        var set = CurrentLessonSet.Available(new HashSet<LessonId> { Lesson });

        Assert.Equal(CurrentLessonSetStatus.Available, set.Status);
        Assert.Equal([Lesson], set.LessonIds);
    }

    [Fact]
    public void Available_ConConjuntoVacio_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => CurrentLessonSet.Available(new HashSet<LessonId>()));
    }

    [Fact]
    public void NotAvailable_YUnknown_LlevanConjuntoVacio()
    {
        Assert.Equal(CurrentLessonSetStatus.NotAvailable, CurrentLessonSet.NotAvailable.Status);
        Assert.Empty(CurrentLessonSet.NotAvailable.LessonIds);

        Assert.Equal(CurrentLessonSetStatus.Unknown, CurrentLessonSet.Unknown.Status);
        Assert.Empty(CurrentLessonSet.Unknown.LessonIds);
    }

    [Fact]
    public void CurrentLessonSet_NoExponeConstructorPublico()
    {
        Assert.Empty(typeof(CurrentLessonSet).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }
}
