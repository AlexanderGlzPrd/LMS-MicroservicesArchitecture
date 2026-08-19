using Asp.Versioning;
using BffComposition.Api.Clients;
using BffComposition.Api.Clients.CourseAuthoring;
using BffComposition.Api.Clients.Learning;
using BffComposition.Api.Composition;
using BffComposition.Api.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddHealthChecks();

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
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
