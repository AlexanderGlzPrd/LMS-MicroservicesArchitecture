using Asp.Versioning;
using BuildingBlocks.Messaging;
using BuildingBlocks.Observability;
using Enrollments.Api.Actor;
using Enrollments.Api.Errors;
using Enrollments.Api.Time;
using Enrollments.Application;
using Enrollments.Application.Abstractions;
using Enrollments.Infrastructure;
using Enrollments.Infrastructure.Acl;
using Enrollments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
builder.AddLmsObservability("enrollment");

var connectionString = builder.Configuration.GetConnectionString("Enrollment")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexion 'Enrollment' en la configuracion.");

if (string.IsNullOrWhiteSpace(
        builder.Configuration[$"{CourseAuthoringOptions.SectionName}:BaseUrl"]))
{
    throw new InvalidOperationException(
        $"Falta '{CourseAuthoringOptions.SectionName}:BaseUrl' en la configuracion.");
}

if (string.IsNullOrWhiteSpace(
        builder.Configuration[$"{RabbitMqOptions.SectionName}:Host"]))
{
    throw new InvalidOperationException(
        $"Falta '{RabbitMqOptions.SectionName}:Host' en la configuracion.");
}

builder.Services.AddSingleton<TimeProvider>(new MicrosecondTimeProvider(TimeProvider.System));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString, builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, HttpCurrentActor>();

var authority = builder.Configuration["Authentication:Authority"];
var metadataAddress = builder.Configuration["Authentication:MetadataAddress"];

if (string.IsNullOrWhiteSpace(authority))
{
    throw new InvalidOperationException(
        "Falta 'Authentication:Authority' en la configuracion.");
}

var audience = builder.Configuration["Authentication:Audience"];

if (string.IsNullOrWhiteSpace(audience))
{
    throw new InvalidOperationException(
        "Falta 'Authentication:Audience' en la configuracion.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;

        if (!string.IsNullOrWhiteSpace(metadataAddress))
        {
            options.MetadataAddress = metadataAddress;
        }

        options.Audience = audience;
        options.RequireHttpsMetadata = builder.Configuration
            .GetValue("Authentication:RequireHttpsMetadata", true);

        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "roles",
            NameClaimType = "sub",
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Student", policy => policy.RequireRole("Student"));
    options.AddPolicy("ServiceAccessReader", policy => policy.RequireRole("access-reader"));

    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

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
    app.MapOpenApi().WithDocumentPerVersion().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.UseLmsCorrelation();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registration => !registration.Tags.Contains("masstransit"),
}).AllowAnonymous();

app.Run();

public partial class Program;
