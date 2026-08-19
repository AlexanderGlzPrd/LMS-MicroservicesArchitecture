using Asp.Versioning;
using BuildingBlocks.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using PaidEnrollment.Api.Actor;
using PaidEnrollment.Api.Errors;
using PaidEnrollment.Api.Time;
using PaidEnrollment.Application;
using PaidEnrollment.Application.Abstractions;
using PaidEnrollment.Infrastructure;
using PaidEnrollment.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

var connectionString = builder.Configuration.GetConnectionString("PaidEnrollment")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexion 'PaidEnrollment' en la configuracion.");

if (string.IsNullOrWhiteSpace(builder.Configuration["Services:Enrollment:BaseUrl"]))
{
    throw new InvalidOperationException(
        "Falta 'Services:Enrollment:BaseUrl' en la configuracion.");
}

foreach (var key in new[] { "ClientId", "ClientSecret", "TokenEndpoint" })
{
    if (string.IsNullOrWhiteSpace(builder.Configuration[$"Services:Enrollment:{key}"]))
    {
        throw new InvalidOperationException(
            $"Falta 'Services:Enrollment:{key}' en la configuracion.");
    }
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
builder.Services.AddScoped<ICurrentOperator, HttpCurrentOperator>();

var authority = builder.Configuration["Authentication:Authority"];

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
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));

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
    .AddDbContextCheck<PaidEnrollmentDbContext>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registration => !registration.Tags.Contains("masstransit"),
}).AllowAnonymous();

app.Run();

public partial class Program;
