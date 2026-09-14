using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using SmartCity.Api.ExceptionHandling;
using SmartCity.Application.Abstractions;
using SmartCity.Application.Configuration;
using SmartCity.Application.Services;
using SmartCity.Infrastructure;
using SmartCity.Infrastructure.OpenStreetMap;
using SmartCity.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Container-friendly logging; avoids platform-specific Windows Event Log permissions.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var connectionString = builder.Configuration.GetConnectionString("SmartCityDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'SmartCityDatabase' was not found.");

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services
    .AddOptions<PilotAreaOptions>()
    .BindConfiguration(PilotAreaOptions.SectionName)
    .Validate(options => options.IsValid(),
        "PilotArea must contain a name, valid latitude/longitude ranges, " +
        "South < North and West < East.")
    .ValidateOnStart();
builder.Services
    .AddOptions<OpenStreetMapOptions>()
    .BindConfiguration(OpenStreetMapOptions.SectionName)
    .Validate(options => options.IsValid(),
        "OpenStreetMap settings contain an invalid URL, timeout or response size limit.")
    .ValidateOnStart();
builder.Services.AddScoped<IImportOpenStreetMapDataService, ImportOpenStreetMapDataService>();
builder.Services.AddScoped<ICoverageAnalysisService, CoverageAnalysisService>();
builder.Services.AddScoped<IAccessibilityAnalysisService, AccessibilityAnalysisService>();
builder.Services.AddScoped<IIncidentPriorityService, IncidentPriorityService>();
builder.Services.AddInfrastructure(connectionString);
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<SmartCityDbContext>(
        name: "postgresql",
        tags: ["ready"]);

var app = builder.Build();
var logMissingFrontend = LoggerMessage.Define<string>(
    LogLevel.Warning,
    new EventId(1001, "FrontendFilesMissing"),
    "Frontend static files were not found at {FrontendPath}");

app.UseExceptionHandler();

// Container/orchestrator probes should remain available over the internal HTTP port.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/health"),
    branch => branch.UseHttpsRedirection());

var frontendPath = Path.Combine(AppContext.BaseDirectory, "frontend");
if (Directory.Exists(frontendPath))
{
    var frontendFiles = new PhysicalFileProvider(frontendPath);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = frontendFiles
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = frontendFiles
    });
}
else
{
    logMissingFrontend(app.Logger, frontendPath, null);
}

app.MapControllers();

// Liveness only verifies that the API process can answer requests.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness also verifies that PostgreSQL/PostGIS is reachable.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.Run();
