using Asp.Versioning;

using Enrollments.Api.Actor;
using Enrollments.Api.Errors;
using Enrollments.Application;
using Enrollments.Application.Abstractions;
using Enrollments.Infrastructure;
using Enrollments.Infrastructure.Acl;
using Enrollments.Infrastructure.Persistence;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

var connectionString = builder.Configuration.GetConnectionString("Enrollment")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexion 'Enrollment' en la configuracion.");

if (string.IsNullOrWhiteSpace(
        builder.Configuration[$"{CourseAuthoringOptions.SectionName}:BaseUrl"]))
{
    throw new InvalidOperationException(
        $"Falta '{CourseAuthoringOptions.SectionName}:BaseUrl' en la configuracion.");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString, builder.Configuration);

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

builder.Services.AddHealthChecks()
    .AddDbContextCheck<EnrollmentsDbContext>();

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

public partial class Program;
