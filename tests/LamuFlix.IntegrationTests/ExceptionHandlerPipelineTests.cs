using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class ExceptionHandlerPipelineTests
{
    private const string ProbePath = "/exception-handler-probe";
    private const string SentinelMessage = "sentinel-non-domain-exception";
    private const string ProblemDetailsJson = "application/problem+json";
    private const string HandledExceptionEvent = "Microsoft.AspNetCore.Diagnostics.HandledException";

    [Fact]
    public async Task Get_NonDomainException_ReturnsProblemDetailsAndRecordsExceptionDiagnostics()
    {
        // arrange
        var observed = new HandledExceptionObserver();
        await using WebApplicationFactory<Program> host = new ApiHostFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(static services =>
                services.AddSingleton<IStartupFilter, NonDomainExceptionStartupFilter>()));
        using var client = host.CreateClient();
        using var subscription = host.Services.GetRequiredService<DiagnosticListener>().Subscribe(observed);

        // act
        var response = await client.GetAsync(ProbePath, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.InternalServerError);
        problem.GetProperty("title").GetString().ShouldBe("Internal Server Error");
        problem.GetProperty("type").GetString().ShouldBe("https://tools.ietf.org/html/rfc9110#section-15.6.1");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(SentinelMessage);
        body.ShouldNotContain(nameof(InvalidOperationException));
        body.ShouldNotContain("stackTrace");
        observed.Exceptions.ShouldContain(exception =>
            exception is InvalidOperationException && exception.Message == SentinelMessage);
    }

    private sealed class NonDomainExceptionStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            ArgumentNullException.ThrowIfNull(next);
            return app =>
            {
                next(app);
                app.Use(static (context, nextMiddleware) =>
                {
                    if (!context.Request.Path.Equals(ProbePath, StringComparison.Ordinal))
                    {
                        return nextMiddleware(context);
                    }

                    throw new InvalidOperationException(SentinelMessage);
                });
            };
        }
    }

    private sealed class HandledExceptionObserver : IObserver<KeyValuePair<string, object?>>
    {
        private readonly ConcurrentQueue<Exception> exceptions = new();

        public ConcurrentQueue<Exception> Exceptions => exceptions;

        public void OnCompleted()
        {
            // The listener stays open until the request finishes.
        }

        public void OnError(Exception error)
        {
            // Delivery failures are outside the diagnostic being asserted.
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key != HandledExceptionEvent || value.Value is null)
            {
                return;
            }

            var exception = value.Value.GetType().GetProperty("exception")?.GetValue(value.Value) as Exception;
            if (exception is not null)
            {
                exceptions.Enqueue(exception);
            }
        }
    }
}
