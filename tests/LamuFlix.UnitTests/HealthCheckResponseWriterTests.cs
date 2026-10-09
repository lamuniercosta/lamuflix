using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LamuFlix.ServiceDefaults;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LamuFlix.UnitTests;

public sealed class HealthCheckResponseWriterTests
{
    private const string Postgres = "postgres";
    private const string RabbitMq = "rabbitmq";

    [Fact]
    public async Task WriteAsync_NullContext_Throws()
    {
        // arrange
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            HealthStatus.Healthy,
            TimeSpan.Zero);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => HealthCheckResponseWriter.WriteAsync(null!, report);

        // assert
        (await act.ShouldThrowAsync<ArgumentNullException>()).ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task WriteAsync_NullReport_Throws()
    {
        // arrange
        var context = new DefaultHttpContext();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => HealthCheckResponseWriter.WriteAsync(context, null!);

        // assert
        (await act.ShouldThrowAsync<ArgumentNullException>()).ParamName.ShouldBe("report");
    }

    [Theory]
    [InlineData(HealthStatus.Healthy)]
    [InlineData(HealthStatus.Degraded)]
    public async Task WriteAsync_NotUnhealthy_WritesJsonStatusAndChecks(HealthStatus status)
    {
        // arrange
        var context = Context();
        var report = Report(
            status,
            (Postgres, Entry(status, TimeSpan.FromMilliseconds(42))),
            (RabbitMq, Entry(HealthStatus.Healthy, TimeSpan.FromMilliseconds(7))));

        // act
        await HealthCheckResponseWriter.WriteAsync(context, report);
        var body = await ReadBody(context);

        // assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        body.GetProperty("status").GetString().ShouldBe(status.ToString());
        var checks = body.GetProperty("checks");
        checks.GetArrayLength().ShouldBe(2);
        Check(checks, Postgres).GetProperty("status").GetString().ShouldBe(status.ToString());
        Check(checks, Postgres).GetProperty("durationMs").GetInt32().ShouldBe(42);
        Check(checks, RabbitMq).GetProperty("status").GetString().ShouldBe(nameof(HealthStatus.Healthy));
        Check(checks, RabbitMq).GetProperty("durationMs").GetInt32().ShouldBe(7);
    }

    [Fact]
    public async Task WriteAsync_Unhealthy_WritesProblemDetailsWithChecks()
    {
        // arrange
        var context = Context();
        var report = Report(
            HealthStatus.Unhealthy,
            (Postgres, Entry(HealthStatus.Unhealthy, TimeSpan.FromMilliseconds(15))),
            (RabbitMq, Entry(HealthStatus.Healthy, TimeSpan.FromMilliseconds(3))));

        // act
        await HealthCheckResponseWriter.WriteAsync(context, report);
        var body = await ReadBody(context);

        // assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        context.Response.ContentType.ShouldNotBeNull().ShouldStartWith("application/problem+json");
        body.GetProperty("title").GetString().ShouldBe("Service Unavailable");
        body.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status503ServiceUnavailable);
        var checks = body.GetProperty("checks");
        checks.GetArrayLength().ShouldBe(2);
        Check(checks, Postgres).GetProperty("status").GetString().ShouldBe(nameof(HealthStatus.Unhealthy));
        Check(checks, Postgres).GetProperty("durationMs").GetInt32().ShouldBe(15);
        Check(checks, RabbitMq).GetProperty("durationMs").GetInt32().ShouldBe(3);
    }

    private static DefaultHttpContext Context()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        return new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() },
        };
    }

    private static HealthReport Report(
        HealthStatus status,
        (string Name, HealthReportEntry Entry) first,
        (string Name, HealthReportEntry Entry) second) =>
        new(
            new Dictionary<string, HealthReportEntry>
            {
                [first.Name] = first.Entry,
                [second.Name] = second.Entry,
            },
            status,
            first.Entry.Duration + second.Entry.Duration);

    private static HealthReportEntry Entry(HealthStatus status, TimeSpan duration) =>
        new(status, description: null, duration, exception: null, data: null);

    private static async Task<JsonElement> ReadBody(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        return document.RootElement.Clone();
    }

    private static JsonElement Check(JsonElement checks, string name) =>
        checks.EnumerateArray().Single(check => check.GetProperty("name").GetString() == name);
}
