using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Api;
using LamuFlix.Api.ExceptionHandling;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace LamuFlix.UnitTests.ExceptionHandling;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ValidationException_Writes422WithoutExceptionLeak()
    {
        // arrange
        var handler = CreateHandler(out var context);
        var exception = CreateValidationException();

        // act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // assert
        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        using var document = await ReadBody(context);
        var root = document.RootElement;
        root.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status422UnprocessableEntity);
        root.GetProperty("title").GetString().ShouldBe("Unprocessable Entity");
        root.GetProperty("type").GetString().ShouldBe("https://tools.ietf.org/html/rfc9110#section-15.5.21");
        root.GetProperty("traceId").GetString().ShouldBe("trace-296");
        root.GetProperty("errors").GetProperty("Title")[0].GetString().ShouldBe("required");
        var json = root.GetRawText();
        json.ShouldNotContain("Validation failed.");
        json.ShouldNotContain(nameof(ValidationException));
    }

    [Fact]
    public async Task TryHandleAsync_NotFoundException_Writes404WithoutExceptionLeak()
    {
        // arrange
        var handler = CreateHandler(out var context);

        // act
        var handled = await handler.TryHandleAsync(context, new NotFoundException(), CancellationToken.None);

        // assert
        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        using var document = await ReadBody(context);
        var root = document.RootElement;
        root.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status404NotFound);
        root.GetProperty("title").GetString().ShouldBe("Not Found");
        root.GetProperty("type").GetString().ShouldBe("https://tools.ietf.org/html/rfc9110#section-15.5.5");
        root.GetProperty("traceId").GetString().ShouldBe("trace-296");
        var json = root.GetRawText();
        json.ShouldNotContain("Not found.");
        json.ShouldNotContain(nameof(NotFoundException));
    }

    [Fact]
    public async Task TryHandleAsync_InvalidTransitionException_Writes409WithoutExceptionLeak()
    {
        // arrange
        var handler = CreateHandler(out var context);

        // act
        var handled = await handler.TryHandleAsync(
            context,
            new InvalidTransitionException("Retry", "Enriched"),
            CancellationToken.None);

        // assert
        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        using var document = await ReadBody(context);
        var root = document.RootElement;
        root.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status409Conflict);
        root.GetProperty("title").GetString().ShouldBe("Conflict");
        root.GetProperty("type").GetString().ShouldBe("https://tools.ietf.org/html/rfc9110#section-15.5.10");
        root.GetProperty("traceId").GetString().ShouldBe("trace-296");
        AssertNoLeak(root, "Cannot Retry from Enriched.", nameof(InvalidTransitionException));
    }

    [Fact]
    public async Task TryHandleAsync_FeatureDisabledException_Writes403WithoutExceptionLeak()
    {
        // arrange
        var handler = CreateHandler(out var context);

        // act
        var handled = await handler.TryHandleAsync(
            context,
            new FeatureDisabledException("local-play-off"),
            CancellationToken.None);

        // assert
        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        using var document = await ReadBody(context);
        var root = document.RootElement;
        root.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status403Forbidden);
        root.GetProperty("title").GetString().ShouldBe("Forbidden");
        root.GetProperty("type").GetString().ShouldBe("https://tools.ietf.org/html/rfc9110#section-15.5.4");
        root.GetProperty("traceId").GetString().ShouldBe("trace-296");
        AssertNoLeak(root, "local-play-off", nameof(FeatureDisabledException));
    }

    [Fact]
    public async Task TryHandleAsync_UnhandledException_Writes500ProblemDetailsWithoutStackLeak()
    {
        // arrange
        var handler = CreateHandler(out var context);

        // act
        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("boom-secret"),
            CancellationToken.None);

        // assert
        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.ShouldNotBeNull().ShouldStartWith("application/problem+json");
        using var document = await ReadBody(context);
        var root = document.RootElement;
        root.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status500InternalServerError);
        root.GetProperty("title").GetString().ShouldBe("Internal Server Error");
        root.GetProperty("type").GetString().ShouldBe("https://tools.ietf.org/html/rfc9110#section-15.6.1");
        root.GetProperty("traceId").GetString().ShouldBe("trace-296");
        root.TryGetProperty("detail", out _).ShouldBeFalse();
        AssertNoLeak(root, "boom-secret", nameof(InvalidOperationException));
    }

    [Fact]
    public void JsonOptions_SerializeEnrichmentStatusAndEnumsAsStrings()
    {
        // arrange
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        HttpJsonConfiguration.Apply(options);

        // act
        var status = JsonSerializer.Serialize(EnrichmentStatus.Failed, options);
        var enumerated = JsonSerializer.Serialize(SampleStatus.Pending, options);

        // assert
        status.ShouldBe("\"Failed\"");
        enumerated.ShouldBe("\"Pending\"");
        JsonSerializer.Deserialize<EnrichmentStatus>(status, options).ShouldBe(EnrichmentStatus.Failed);
    }

    [Fact]
    public async Task TryHandleAsync_NullHttpContext_ThrowsArgumentNullException()
    {
        // arrange
        var handler = CreateHandler(out _);
        var exception = CreateValidationException();

        // act
        var thrown = await Should.ThrowAsync<ArgumentNullException>(
            // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
            () => handler.TryHandleAsync(null!, exception, CancellationToken.None).AsTask());

        // assert
        thrown.ParamName.ShouldBe("httpContext");
    }

    [Fact]
    public async Task TryHandleAsync_NullException_ThrowsArgumentNullException()
    {
        // arrange
        var handler = CreateHandler(out var context);

        // act
        var thrown = await Should.ThrowAsync<ArgumentNullException>(
            // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
            () => handler.TryHandleAsync(context, null!, CancellationToken.None).AsTask());

        // assert
        thrown.ParamName.ShouldBe("exception");
    }

    [Fact]
    public async Task TryHandleAsync_CanceledToken_ThrowsOperationCanceledException()
    {
        // arrange
        var handler = CreateHandler(out var context);
        var exception = CreateValidationException();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.TryHandleAsync(context, exception, cancellationTokenSource.Token).AsTask());
    }

    private enum SampleStatus
    {
        Pending,
    }

    private static void AssertNoLeak(JsonElement root, string message, string typeName)
    {
        var json = root.GetRawText();
        json.ShouldNotContain(message);
        json.ShouldNotContain(typeName);
        json.ShouldNotContain("stackTrace");
    }

    private static GlobalExceptionHandler CreateHandler(out DefaultHttpContext context)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ProblemDetailsOptions()));
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new JsonOptions()));
        services.AddProblemDetails();
        var provider = services.BuildServiceProvider();
        context = new DefaultHttpContext
        {
            RequestServices = provider,
            TraceIdentifier = "trace-296",
        };
        context.Response.Body = new MemoryStream();
        return new GlobalExceptionHandler(provider.GetRequiredService<IProblemDetailsService>());
    }

    private static ValidationException CreateValidationException() =>
        new(new Dictionary<string, string[]>
        {
            ["Title"] = ["required"],
        });

    private static async Task<JsonDocument> ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
