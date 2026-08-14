using System.Reflection;
using Learning.Domain.Progress;
using Learning.Domain.Progress.Exceptions;
namespace Learning.Domain.Tests;

public sealed class CourseProgressTests
{
    private static readonly StudentId Student = new(Guid.CreateVersion7());
    private static readonly CourseId Course = new(Guid.CreateVersion7());
    private static readonly LessonId First = new(Guid.CreateVersion7());
    private static readonly LessonId Second = new(Guid.CreateVersion7());
    private static readonly LessonId Bonus = new(Guid.CreateVersion7());
    private static readonly DateTimeOffset StartedAt = new(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FirstMark = new(2026, 8, 13, 11, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SecondMark = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ThirdMark = new(2026, 8, 13, 13, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlySet<LessonId> Published = new HashSet<LessonId> { First, Second };
    private static readonly IReadOnlySet<LessonId> PublishedConBonus = new HashSet<LessonId> { First, Second, Bonus };
    private static readonly IReadOnlySet<LessonId> Vacio = new HashSet<LessonId>();

    [Fact]
    public void Start_DejaElProgresoEnCursoYSinLeccionesCompletadas()
    {
        var progress = CourseProgress.Start(Student, Course, StartedAt);

        Assert.Equal(Student, progress.StudentId);
        Assert.Equal(Course, progress.CourseId);
        Assert.Equal(CourseProgressStatus.InProgress, progress.Status);
        Assert.Equal(StartedAt, progress.StartedAt);
        Assert.Null(progress.CompletedAt);
        Assert.Empty(progress.CompletedLessons);
    }

    [Fact]
    public void Start_ConStudentIdVacio_LanzaExcepcionDeDominio()
    {
        Assert.Throws<InvalidLearningIdentityException>(
            () => CourseProgress.Start(new StudentId(Guid.Empty), Course, StartedAt));
    }

    [Fact]
    public void Start_ConCourseIdVacio_LanzaExcepcionDeDominio()
    {
        Assert.Throws<InvalidLearningIdentityException>(
            () => CourseProgress.Start(Student, new CourseId(Guid.Empty), StartedAt));
    }

    [Fact]
    public void MarkLessonCompleted_ConLessonIdVacio_LanzaExcepcionDeDominio()
    {
        var progress = CourseProgress.Start(Student, Course, StartedAt);

        Assert.Throws<InvalidLearningIdentityException>(
            () => progress.MarkLessonCompleted(new LessonId(Guid.Empty), Published, FirstMark));

        Assert.Empty(progress.CompletedLessons);
    }

    [Fact]
    public void MarkLessonCompleted_ConLeccionFueraDelConjunto_LanzaYNoModificaNada()
    {
        var progress = CourseProgress.Start(Student, Course, StartedAt);

        Assert.Throws<LessonNotInPublishedContentException>(
            () => progress.MarkLessonCompleted(Bonus, Published, FirstMark));

        Assert.Empty(progress.CompletedLessons);
        Assert.Equal(CourseProgressStatus.InProgress, progress.Status);
        Assert.Null(progress.CompletedAt);
    }

    [Fact]
    public void MarkLessonCompleted_ConConjuntoPublicadoVacio_LanzaYNuncaSella()
    {
        var progress = CourseProgress.Start(Student, Course, StartedAt);

        Assert.Throws<LessonNotInPublishedContentException>(
            () => progress.MarkLessonCompleted(First, Vacio, FirstMark));

        Assert.Empty(progress.CompletedLessons);
        Assert.Equal(CourseProgressStatus.InProgress, progress.Status);
        Assert.Null(progress.CompletedAt);
    }

    [Fact]
    public void MarkLessonCompleted_DeUnaLeccionIntermedia_DejaElProgresoEnCurso()
    {
        var progress = CourseProgress.Start(Student, Course, StartedAt);

        var changed = progress.MarkLessonCompleted(First, Published, FirstMark);

        Assert.True(changed);
        Assert.Equal(CourseProgressStatus.InProgress, progress.Status);
        Assert.Null(progress.CompletedAt);

        var completed = Assert.Single(progress.CompletedLessons);

        Assert.Equal(First, completed.LessonId);
        Assert.Equal(FirstMark, completed.CompletedAt);
    }

    [Fact]
    public void MarkLessonCompleted_RepetidoConLaMismaLeccion_EsIdempotente()
    {
        var progress = CourseProgress.Start(Student, Course, StartedAt);

        Assert.True(progress.MarkLessonCompleted(First, Published, FirstMark));
        Assert.False(progress.MarkLessonCompleted(First, Published, SecondMark));

        var completed = Assert.Single(progress.CompletedLessons);

        Assert.Equal(FirstMark, completed.CompletedAt);
        Assert.Equal(CourseProgressStatus.InProgress, progress.Status);
    }

    [Fact]
    public void MarkLessonCompleted_DeLaUltimaLeccionPendiente_SellaLaFinalizacion()
    {
        var progress = CourseProgress.Start(Student, Course, StartedAt);

        progress.MarkLessonCompleted(First, Published, FirstMark);
        var changed = progress.MarkLessonCompleted(Second, Published, SecondMark);

        Assert.True(changed);
        Assert.Equal(CourseProgressStatus.Completed, progress.Status);
        Assert.Equal(SecondMark, progress.CompletedAt);
        Assert.Equal(2, progress.CompletedLessons.Count);
    }

    [Fact]
    public void MarkLessonCompleted_DeContenidoBonusTrasElSellado_RegistraSinTocarElSello()
    {
        var progress = Sellado();

        var changed = progress.MarkLessonCompleted(Bonus, PublishedConBonus, ThirdMark);

        Assert.True(changed);
        Assert.Equal(CourseProgressStatus.Completed, progress.Status);
        Assert.Equal(SecondMark, progress.CompletedAt);
        Assert.Equal(3, progress.CompletedLessons.Count);
    }

    [Fact]
    public void MarkLessonCompleted_RepetidoTrasElSellado_NoReescribeElSello()
    {
        var progress = Sellado();

        Assert.False(progress.MarkLessonCompleted(First, Published, ThirdMark));

        Assert.Equal(CourseProgressStatus.Completed, progress.Status);
        Assert.Equal(SecondMark, progress.CompletedAt);
        Assert.Equal(2, progress.CompletedLessons.Count);
    }

    [Fact]
    public void ConfirmCompletion_SinCumplirElCriterio_LanzaYNoModificaNada()
    {
        var progress = CourseProgress.Start(Student, Course, StartedAt);
        progress.MarkLessonCompleted(First, Published, FirstMark);

        Assert.Throws<CompletionNotReadyException>(
            () => progress.ConfirmCompletion(Published, ThirdMark));

        Assert.Equal(CourseProgressStatus.InProgress, progress.Status);
        Assert.Null(progress.CompletedAt);
        Assert.Single(progress.CompletedLessons);
    }

    [Fact]
    public void ConfirmCompletion_ConConjuntoPublicadoVacio_LanzaYNuncaSella()
    {
        var progress = CourseProgress.Start(Student, Course, StartedAt);

        Assert.Throws<CompletionNotReadyException>(
            () => progress.ConfirmCompletion(Vacio, ThirdMark));

        Assert.Equal(CourseProgressStatus.InProgress, progress.Status);
        Assert.Null(progress.CompletedAt);
    }

    [Fact]
    public void ConfirmCompletion_CumpliendoElCriterio_SellaLaFinalizacion()
    {
        var progress = CourseProgress.Start(Student, Course, StartedAt);

        progress.MarkLessonCompleted(First, PublishedConBonus, FirstMark);
        progress.MarkLessonCompleted(Second, PublishedConBonus, SecondMark);

        Assert.Equal(CourseProgressStatus.InProgress, progress.Status);

        var justSealed = progress.ConfirmCompletion(Published, ThirdMark);

        Assert.True(justSealed);
        Assert.Equal(CourseProgressStatus.Completed, progress.Status);
        Assert.Equal(ThirdMark, progress.CompletedAt);
    }

    [Fact]
    public void ConfirmCompletion_SobreUnProgresoYaSellado_EsIdempotente()
    {
        var progress = Sellado();

        Assert.False(progress.ConfirmCompletion(Published, ThirdMark));

        Assert.Equal(CourseProgressStatus.Completed, progress.Status);
        Assert.Equal(SecondMark, progress.CompletedAt);
    }

    [Fact]
    public void ConfirmCompletion_NoFabricaLeccionesCompletadas()
    {
        var progress = CourseProgress.Start(Student, Course, StartedAt);

        progress.MarkLessonCompleted(First, Published, FirstMark);
        progress.MarkLessonCompleted(Second, Published, SecondMark);

        progress.ConfirmCompletion(Published, ThirdMark);

        Assert.Equal(2, progress.CompletedLessons.Count);
    }

    [Fact]
    public void CourseProgress_SoloExponeTresComportamientos_YNingunoRevierteElSello()
    {
        var comportamientos = typeof(CourseProgress)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .OrderBy(name => name)
            .ToList();

        Assert.Equal(["ConfirmCompletion", "MarkLessonCompleted", "Start"], comportamientos);
    }

    [Fact]
    public void CourseProgress_NoExponeSettersPublicos()
    {
        var setters = typeof(CourseProgress)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.SetMethod)
            .Where(setter => setter is not null && setter.IsPublic);

        Assert.Empty(setters);
    }

    [Fact]
    public void CourseProgress_NoAlmacenaElConjuntoDeLeccionesPublicadas()
    {
        var almacenado = typeof(CourseProgress)
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Select(field => field.FieldType)
            .Concat(typeof(CourseProgress)
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Select(property => property.PropertyType));

        Assert.DoesNotContain(
            almacenado,
            type => type.IsGenericType && type.GetGenericArguments().Contains(typeof(LessonId)));
    }

    [Fact]
    public void CompletedLesson_SoloLaFabricaElAgregado()
    {
        Assert.Empty(typeof(CompletedLesson).GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        var sinParametros = typeof(CompletedLesson)
            .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes);

        Assert.NotNull(sinParametros);
        Assert.True(sinParametros.IsPrivate);

        var create = typeof(CompletedLesson).GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(create);
        Assert.True(create.IsAssembly);
    }

    private static CourseProgress Sellado()
    {
        var progress = CourseProgress.Start(Student, Course, StartedAt);

        progress.MarkLessonCompleted(First, Published, FirstMark);
        progress.MarkLessonCompleted(Second, Published, SecondMark);

        return progress;
    }
}
