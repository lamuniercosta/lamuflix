using LamuFlix.Api.ExceptionHandling;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddLamuFlixPersistence(builder.Configuration);
builder.Services.AddLamuFlixRabbitMq();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
var app = builder.Build();
app.UseExceptionHandler();
app.Run();
