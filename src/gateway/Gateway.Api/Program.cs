using BuildingBlocks.Observability;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Transforms;
var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
builder.AddLmsObservability("gateway");

var authority = builder.Configuration["Authentication:Authority"];
var metadataAddress = builder.Configuration["Authentication:MetadataAddress"];

if (string.IsNullOrWhiteSpace(authority))
{
    throw new InvalidOperationException(
        "Falta 'Authentication:Authority' en la configuracion.");
}

var validAudiences = builder.Configuration
    .GetSection("Authentication:ValidAudiences")
    .Get<string[]>() ?? [];

if (validAudiences.Length == 0)
{
    throw new InvalidOperationException(
        "Falta 'Authentication:ValidAudiences' en la configuracion.");
}

var reverseProxySection = builder.Configuration.GetSection("ReverseProxy");

if (!reverseProxySection.Exists())
{
    throw new InvalidOperationException(
        "Falta la seccion 'ReverseProxy' en la configuracion.");
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

        options.RequireHttpsMetadata = builder.Configuration
            .GetValue("Authentication:RequireHttpsMetadata", true);

        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudiences = validAudiences,
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
    options.AddPolicy("Instructor", policy => policy.RequireRole("Instructor"));
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(reverseProxySection)
    .AddTransforms(context =>
    {
        context.AddRequestHeaderRemove("X-Student-Id");
        context.AddRequestHeaderRemove("X-Instructor-Id");
        context.AddRequestHeaderRemove("X-Operator-Id");
    });

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;

    var (title, detail) = httpContext.Response.StatusCode switch
    {
        StatusCodes.Status401Unauthorized => (
            "No autenticado",
            "La peticion no incluye un token valido para esta ruta."),

        StatusCodes.Status403Forbidden => (
            "No autorizado",
            "El token no tiene el rol requerido por esta ruta."),

        _ => (string.Empty, string.Empty),
    };

    if (title.Length == 0)
    {
        return;
    }

    var problemDetailsService = httpContext.RequestServices
        .GetRequiredService<IProblemDetailsService>();

    await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
    {
        HttpContext = httpContext,
        ProblemDetails =
        {
            Status = httpContext.Response.StatusCode,
            Title = title,
            Detail = detail,
        },
    });
});

app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (context.GetEndpoint() is null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next(context);

    if (context.Response.StatusCode == StatusCodes.Status405MethodNotAllowed
        && !context.Response.HasStarted)
    {
        context.Response.Headers.Remove("Allow");
        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }
});

app.UseAuthorization();

app.MapReverseProxy();

app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
