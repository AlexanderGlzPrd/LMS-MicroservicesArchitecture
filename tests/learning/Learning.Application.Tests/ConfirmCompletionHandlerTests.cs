using Learning.Application.Abstractions;
using Learning.Application.Abstractions.Exceptions;
using Learning.Application.Progress;
using Learning.Application.Progress.ConfirmCompletion;
using Learning.Application.Tests.Fakes;
using Learning.Domain.Progress;
using Learning.Domain.Progress.Exceptions;
namespace Learning.Application.Tests;

public sealed class ConfirmCompletionHandlerTests
{
    private static readonly StudentId Student = new(Guid.CreateVersion7());
    private static readonly CourseId Course = new(Guid.CreateVersion7());
    private static readonly LessonId First = new(Guid.CreateVersion7());
    private static readonly LessonId Second = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset Antes = new(2026, 8, 14, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlySet<LessonId> Publicadas = new HashSet<LessonId> { First, Second };

    private readonly InMemoryCourseProgressRepository progresses = new();
    private readonly StubCurrentActor currentActor = new(Student);
    private readonly FixedTimeProvider timeProvider = new(Now);

    [Fact]
    public async Task Confirmar_SinProgreso_LanzaNotFound_DespuesDeConsultarElConjunto()
    {
        var lessonSet = Disponible();
        var unitOfWork = new NoOpUnitOfWork(progresses);

        await Assert.ThrowsAsync<CourseProgressNotFoundException>(
            () => HandleAsync(lessonSet, unitOfWork));

        Assert.Equal(1, lessonSet.GetCount);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Confirmar_ConCursoNoDisponible_LanzaAunqueElProgresoNoExista()
    {
        var unitOfWork = new NoOpUnitOfWork(progresses);

        await Assert.ThrowsAsync<CourseNotAvailableException>(
            () => HandleAsync(new StubCurrentLessonSet(CurrentLessonSet.NotAvailable), unitOfWork));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Confirmar_ConConjuntoDesconocido_LanzaAunqueElProgresoNoExista()
    {
        var unitOfWork = new NoOpUnitOfWork(progresses);

        await Assert.ThrowsAsync<CurrentLessonSetUnknownException>(
            () => HandleAsync(new StubCurrentLessonSet(CurrentLessonSet.Unknown), unitOfWork));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Confirmar_SinCumplirElCriterio_LanzaYNoPersiste()
    {
        progresses.Seed(ProgresoCon(First));

        var unitOfWork = new NoOpUnitOfWork(progresses);

        await Assert.ThrowsAsync<CompletionNotReadyException>(
            () => HandleAsync(Disponible(), unitOfWork));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Confirmar_CumpliendoElCriterio_SellaYPersisteUnaVez()
    {
        progresses.Seed(ProgresoCon(First));

        var vigente = new StubCurrentLessonSet(
            CurrentLessonSet.Available(new HashSet<LessonId> { First }));

        var unitOfWork = new NoOpUnitOfWork(progresses);

        var view = await HandleAsync(vigente, unitOfWork);

        Assert.Equal(nameof(CourseProgressStatus.Completed), view.Status);
        Assert.Equal(Now, view.CompletedAt);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Confirmar_SobreUnProgresoYaSellado_NoPersisteYConservaLaFecha()
    {
        progresses.Seed(ProgresoCon(First, Second));

        var unitOfWork = new NoOpUnitOfWork(progresses);

        var view = await HandleAsync(Disponible(), unitOfWork);

        Assert.Equal(nameof(CourseProgressStatus.Completed), view.Status);
        Assert.Equal(Antes, view.CompletedAt);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Confirmar_NoTieneLogicaDeReintento_LaCarreraSePropaga()
    {
        progresses.Seed(ProgresoCon(First));

        var vigente = new StubCurrentLessonSet(
            CurrentLessonSet.Available(new HashSet<LessonId> { First }));

        var unitOfWork = new NoOpUnitOfWork(progresses)
        {
            ThrowOnSave = new ConcurrentCourseProgressException(new InvalidOperationException()),
        };

        await Assert.ThrowsAsync<ConcurrentCourseProgressException>(
            () => HandleAsync(vigente, unitOfWork));

        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Equal(1, vigente.GetCount);
    }

    private static CourseProgress ProgresoCon(params LessonId[] lessons)
    {
        var progress = CourseProgress.Start(Student, Course, Antes.AddHours(-1));

        foreach (var lesson in lessons)
        {
            progress.MarkLessonCompleted(lesson, Publicadas, Antes);
        }

        return progress;
    }

    private static StubCurrentLessonSet Disponible() =>
        new(CurrentLessonSet.Available(Publicadas));

    private Task<CourseProgressView> HandleAsync(
        StubCurrentLessonSet lessonSet,
        IUnitOfWork unitOfWork) =>
        new ConfirmCompletionHandler(progresses, unitOfWork, lessonSet, currentActor, timeProvider)
            .HandleAsync(new ConfirmCompletionCommand(Course), CancellationToken.None);
}
