using System;
using System.Linq;
using System.Threading.Tasks;
using LamuFlix.Api;
using LamuFlix.Api.Endpoints;
using LamuFlix.Api.ExceptionHandling;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.Enrichment;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Playback;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

const string CorsPolicyName = "DevSpa";
const string DevServerOrigin = "http://localhost:5173";
const string ApiRoutePrefix = "/api";

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddLamuFlixPersistence(builder.Configuration);
builder.Services.AddMovieCatalog();
builder.Services.AddMetadataProvider();
builder.Services.AddLamuFlixRabbitMq();
builder.Services.AddLamuFlixPlayback();
builder.Services.AddLamuFlixHandlers();
builder.Services.AddHostedService<StrandedMovieSweeper>();
builder.Services.AddCors(options => options.AddPolicy(
    CorsPolicyName,
    policy => policy.WithOrigins(DevServerOrigin).AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("Location")));
builder.Services.ConfigureHttpJsonOptions(options => HttpJsonConfiguration.Apply(options.SerializerOptions));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi("v1", static options => options.AddDocumentTransformer(
    static (document, _, _) =>
    {
        foreach (var path in document.Paths.Keys.Where(static path => !IsApiBusinessPath(path)).ToList())
        {
            document.Paths.Remove(path);
        }

        return Task.CompletedTask;
    }));
var app = builder.Build();
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    SuppressDiagnosticsCallback = static _ => false,
});
app.UseStatusCodePages();
app.UseCors(CorsPolicyName);
app.MapDefaultEndpoints();
app.MapApiEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();

static bool IsApiBusinessPath(string path) =>
    string.Equals(path, ApiRoutePrefix, StringComparison.Ordinal)
    || path.StartsWith(ApiRoutePrefix + "/", StringComparison.Ordinal);
