using LamuFlix.Api;
using LamuFlix.Api.Endpoints;
using LamuFlix.Api.ExceptionHandling;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Playback;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

const string CorsPolicyName = "DevSpa";
const string DevServerOrigin = "http://localhost:5173";

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddLamuFlixPersistence(builder.Configuration);
builder.Services.AddMovieCatalog();
builder.Services.AddMetadataProvider();
builder.Services.AddLamuFlixRabbitMq();
builder.Services.AddLamuFlixPlayback();
builder.Services.AddLamuFlixHandlers();
builder.Services.AddCors(options => options.AddPolicy(
    CorsPolicyName,
    policy => policy.WithOrigins(DevServerOrigin).AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("Location")));
builder.Services.ConfigureHttpJsonOptions(options => HttpJsonConfiguration.Apply(options.SerializerOptions));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
var app = builder.Build();
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    SuppressDiagnosticsCallback = static _ => false,
});
app.UseStatusCodePages();
app.UseCors(CorsPolicyName);
app.MapDefaultEndpoints();
app.MapApiEndpoints();
app.Run();
