using LamuFlix.Api;
using LamuFlix.Api.Endpoints;
using LamuFlix.Api.ExceptionHandling;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.Enrichment;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Playback;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Infrastructure.Thumbnails;
using LamuFlix.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

const string CorsPolicyName = "DevSpa";
const string DevServerOrigin = "http://localhost:5173";

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddLamuFlixPersistence(builder.Configuration);
builder.Services.AddMovieCatalog();
builder.Services.AddMetadataProvider();
builder.Services.AddOllamaEmbeddingGenerator();
builder.Services.AddLamuFlixRabbitMq();
builder.Services.AddLamuFlixPlayback();
builder.Services.AddLamuFlixThumbnails();
builder.Services.AddLamuFlixHandlers();
builder.Services.AddLamuFlixOutboxRelay();
builder.Services.AddHostedService<StrandedMovieSweeper>();
builder.Services.AddCors(options => options.AddPolicy(
    CorsPolicyName,
    policy => policy.WithOrigins(DevServerOrigin).AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("Location")));
builder.Services.ConfigureHttpJsonOptions(options => HttpJsonConfiguration.Apply(options.SerializerOptions));
builder.Services.AddSignalR()
    .AddJsonProtocol(static options => HttpJsonConfiguration.Apply(options.PayloadSerializerOptions));
builder.Services.AddSingleton<IEnrichmentStatusNotifier, SignalREnrichmentStatusNotifier>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi("v1", static options =>
{
    options.AddDocumentTransformer(
        static (document, _, _) => OpenApiDocumentNormalizer.NormalizeAsync(document));
    options.AddSchemaTransformer<EnrichmentStatusSchemaTransformer>();
    options.AddDocumentTransformer<EnrichmentHubSchemaTransformer>();
    options.AddOperationTransformer<BrowseMoviesOperationTransformer>();
    options.AddOperationTransformer<RecommendationsOperationTransformer>();
});
var app = builder.Build();
if (app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value.ApplyMigrationsOnStartup)
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<LamuFlixDbContext>().Database.Migrate();
}

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    SuppressDiagnosticsCallback = static _ => false,
});
app.UseStatusCodePages();
app.UseCors(CorsPolicyName);
app.MapDefaultEndpoints();
app.MapApiEndpoints();
app.MapHub<EnrichmentHub>("/hubs/enrichment");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
