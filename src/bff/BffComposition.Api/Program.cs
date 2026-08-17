using Asp.Versioning;
using BffComposition.Api.Actor;
using BffComposition.Api.Clients;
using BffComposition.Api.Clients.CourseAuthoring;
using BffComposition.Api.Clients.Learning;
using BffComposition.Api.Composition;
using BffComposition.Api.Errors;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

//Validacion de configuracion, no de conectividad: el BFF arranca con las dependencias caidas
if (string.IsNullOrWhiteSpace(
        builder.Configuration[$"{LearningOptions.SectionName}:BaseUrl"]))
{
    throw new InvalidOperationException(
        $"Falta '{LearningOptions.SectionName}:BaseUrl' en la configuracion.");
}

if (string.IsNullOrWhiteSpace(
        builder.Configuration[$"{CourseAuthoringOptions.SectionName}:BaseUrl"]))
{
    throw new InvalidOperationException(
        $"Falta '{CourseAuthoringOptions.SectionName}:BaseUrl' en la configuracion.");
}

builder.Services.Configure<LearningOptions>(
    builder.Configuration.GetSection(LearningOptions.SectionName));
builder.Services.Configure<CourseAuthoringOptions>(
    builder.Configuration.GetSection(CourseAuthoringOptions.SectionName));

builder.Services.AddResilientClient<LearningProgressClient, LearningOptions>("learning");
builder.Services.AddResilientClient<CourseAuthoringCourseClient, CourseAuthoringOptions>(
    "course-authoring");

builder.Services.AddScoped<CoursesInProgressComposer>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, HttpCurrentActor>();

builder.Services.AddControllers();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    })
    .AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
    app.MapScalarApiReference();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
