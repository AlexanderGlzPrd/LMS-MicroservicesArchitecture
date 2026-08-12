using Asp.Versioning;
using CourseAuthoring.Api.Actor;
using CourseAuthoring.Api.Errors;
using CourseAuthoring.Application;
using CourseAuthoring.Application.Abstractions;
using CourseAuthoring.Infrastructure;
using CourseAuthoring.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

var connectionString = builder.Configuration.GetConnectionString("CourseAuthoring")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexion 'CourseAuthoring' en la configuracion.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

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
    .AddDbContextCheck<CourseAuthoringDbContext>();

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

// Punto de entrada visible para WebApplicationFactory en las pruebas de integracion.
public partial class Program;
