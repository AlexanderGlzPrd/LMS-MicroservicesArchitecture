using Learning.Application.Abstractions;
using Learning.Application.Abstractions.Exceptions;
using Learning.Application.Progress;
using Learning.Application.Progress.MarkLessonCompleted;
using Learning.Application.Tests.Fakes;
using Learning.Domain.Progress;
using Learning.Domain.Progress.Exceptions;
namespace Learning.Application.Tests;

public sealed class MarkLessonCompletedHandlerTests
{
    private static readonly StudentId Student = new(Guid.CreateVersion7());
    private static readonly CourseId Course = new(Guid.CreateVersion7());
    private static readonly LessonId First = new(Guid.CreateVersion7());
    private static readonly LessonId Second = new(Guid.CreateVersion7());
    private static readonly LessonId Ajena = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset Antes = new(2026, 8, 14, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlySet<LessonId> Publicadas = new HashSet<LessonId> { First, Second };

    private readonly InMemoryCourseProgressRepository progresses = new();
    private readonly StubCurrentActor currentActor = new(Student);
    private readonly FixedTimeProvider timeProvider = new(Now);

    [Fact]
    public async Task PrimerMarcado_CreaElProgresoYPersisteUnaVez()
    {
        var unitOfWork = new NoOpUnitOfWork(progresses);

        var view = await HandleAsync(First, Disponible(), unitOfWork);

        Assert.Equal(1, progresses.AddCount);
        Assert.Equal(1, progresses.StoredCount);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Equal(nameof(CourseProgressStatus.InProgress), view.Status);
        Assert.Equal([First.Value], view.CompletedLessonIds);
    }

    [Fact]
    public async Task Marcar_ConsultaElConjuntoFrescoAunqueLaLeccionYaEsteCompletada()
    {
        progresses.Seed(ProgresoCon(First));

        var lessonSet = Disponible();

        await HandleAsync(First, lessonSet, new NoOpUnitOfWork(progresses));

        Assert.Equal(1, lessonSet.GetCount);
    }

    [Fact]
    public async Task MarcadoRepetido_NoLlamaASaveChanges()
    {
        progresses.Seed(ProgresoCon(First));

        var unitOfWork = new NoOpUnitOfWork(progresses);

        var view = await HandleAsync(First, Disponible(), unitOfWork);

        Assert.Equal(0, unitOfWork.SaveChangesCount);
        Assert.Equal(0, progresses.AddCount);
        Assert.Equal([First.Value], view.CompletedLessonIds);
    }

    [Fact]
    public async Task Marcar_ConCursoNoDisponible_LanzaYNoPersiste()
    {
        var unitOfWork = new NoOpUnitOfWork(progresses);

        await Assert.ThrowsAsync<CourseNotAvailableException>(
            () => HandleAsync(First, new StubCurrentLessonSet(CurrentLessonSet.NotAvailable), unitOfWork));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
        Assert.Equal(0, progresses.AddCount);
    }

    [Fact]
    public async Task Marcar_ConConjuntoDesconocido_LanzaYNoPersiste()
    {
        var unitOfWork = new NoOpUnitOfWork(progresses);

        await Assert.ThrowsAsync<CurrentLessonSetUnknownException>(
            () => HandleAsync(First, new StubCurrentLessonSet(CurrentLessonSet.Unknown), unitOfWork));

        Assert.Equal(0, unitOfWork.SaveChangesCount);
        Assert.Equal(0, progresses.AddCount);
    }

    [Fact]
    public async Task LeccionFueraDelConjunto_SinProgresoPrevio_NoInvocaAddNiSaveChanges()
    {
        var unitOfWork = new NoOpUnitOfWork(progresses);

        await Assert.ThrowsAsync<LessonNotInPublishedContentException>(
            () => HandleAsync(Ajena, Disponible(), unitOfWork));

        Assert.Equal(0, progresses.AddCount);
        Assert.Equal(0, progresses.StoredCount);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task LeccionFueraDelConjunto_ConProgresoExistente_NoPersiste()
    {
        progresses.Seed(ProgresoCon(First));

        var unitOfWork = new NoOpUnitOfWork(progresses);

        await Assert.ThrowsAsync<LessonNotInPublishedContentException>(
            () => HandleAsync(Ajena, Disponible(), unitOfWork));

        Assert.Equal(0, progresses.AddCount);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Marcar_LaUltimaLeccionPendiente_SellaLaFinalizacion()
    {
        progresses.Seed(ProgresoCon(First));

        var unitOfWork = new NoOpUnitOfWork(progresses);

        var view = await HandleAsync(Second, Disponible(), unitOfWork);

        Assert.Equal(nameof(CourseProgressStatus.Completed), view.Status);
        Assert.Equal(Now, view.CompletedAt);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task ContenidoBonusSobreUnProgresoSellado_SeRegistraSinTocarElSello()
    {
        progresses.Seed(ProgresoCon(First, Second));

        var conBonus = new StubCurrentLessonSet(
            CurrentLessonSet.Available(new HashSet<LessonId> { First, Second, Ajena }));

        var unitOfWork = new NoOpUnitOfWork(progresses);
        var view = await HandleAsync(Ajena, conBonus, unitOfWork);

        Assert.Equal(nameof(CourseProgressStatus.Completed), view.Status);
        Assert.Equal(Antes, view.CompletedAt);
        Assert.Equal(3, view.CompletedLessonIds.Count);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task CarreraPerdida_RecargaYRepiteUnaVez_SinVolverAConsultarElConjunto()
    {
        var lessonSet = Disponible();
        var unitOfWork = new NoOpUnitOfWork(progresses)
        {
            ThrowOnSave = new ConcurrentCourseProgressException(new InvalidOperationException()),
            BeforeThrow = () => progresses.Seed(ProgresoCon(Second)),
        };

        var view = await HandleAsync(First, lessonSet, unitOfWork);

        Assert.Equal(1, lessonSet.GetCount);
        Assert.Equal(2, unitOfWork.SaveChangesCount);
        Assert.Equal(1, progresses.StoredCount);
        Assert.Equal([Second.Value, First.Value], view.CompletedLessonIds);
    }

    [Fact]
    public async Task CarreraPerdidaPorLeccionDuplicada_TerminaEnNoOp()
    {
        var lessonSet = Disponible();
        var unitOfWork = new NoOpUnitOfWork(progresses)
        {
            ThrowOnSave = new ConcurrentCourseProgressException(new InvalidOperationException()),

            BeforeThrow = () => progresses.Seed(ProgresoCon(First)),
        };

        var view = await HandleAsync(First, lessonSet, unitOfWork);

        Assert.Equal(1, lessonSet.GetCount);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Equal(1, progresses.StoredCount);
        Assert.Equal([First.Value], view.CompletedLessonIds);
    }

    [Fact]
    public async Task SegundaCarreraPerdida_SePropaga()
    {
        var lessonSet = Disponible();
        var unitOfWork = new AlwaysFailingUnitOfWork(progresses);

        await Assert.ThrowsAsync<ConcurrentCourseProgressException>(
            () => HandleAsync(First, lessonSet, unitOfWork));

        Assert.Equal(2, unitOfWork.SaveChangesCount);
        Assert.Equal(1, lessonSet.GetCount);
        Assert.Equal(0, progresses.StoredCount);
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
        LessonId lessonId,
        StubCurrentLessonSet lessonSet,
        IUnitOfWork unitOfWork) =>
        new MarkLessonCompletedHandler(progresses, unitOfWork, lessonSet, currentActor, timeProvider)
            .HandleAsync(new MarkLessonCompletedCommand(Course, lessonId), CancellationToken.None);

    private sealed class AlwaysFailingUnitOfWork(InMemoryCourseProgressRepository repository) : IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;
            repository.DiscardPending();

            throw new ConcurrentCourseProgressException(new InvalidOperationException());
        }
    }
}
