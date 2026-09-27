extern alias api;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using api::LamuFlix.Api.ExceptionHandling;
using LamuFlix.Core.Pipeline;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.Test;

public class ValidationExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ValidationException_Writes422WithoutExceptionLeak()
    {
        // arrange
        var handler = CreateHandler(out var context);
        var exception = new ValidationException(new Dictionary<string, string[]>
        {
            ["Title"] = ["required"],
        });

        // act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // assert
        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        using var document = await ReadBody(context);
        var root = document.RootElement;
        root.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status422UnprocessableEntity);
        root.GetProperty("title").GetString().ShouldNotBeNullOrWhiteSpace();
        root.GetProperty("type").GetString().ShouldNotBeNullOrWhiteSpace();
        root.GetProperty("traceId").GetString().ShouldBe("trace-296");
        root.GetProperty("errors").GetProperty("Title")[0].GetString().ShouldBe("required");
        var json = root.GetRawText();
        json.ShouldNotContain("Validation failed.");
        json.ShouldNotContain(nameof(ValidationException));
    }

    [Fact]
    public async Task TryHandleAsync_OtherException_ReturnsFalse()
    {
        // arrange
        var handler = CreateHandler(out var context);

        // act
        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("nope"),
            CancellationToken.None);

        // assert
        handled.ShouldBeFalse();
    }

    private static ValidationExceptionHandler CreateHandler(out DefaultHttpContext context)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new ProblemDetailsOptions()));
        services.AddSingleton(Options.Create(new JsonOptions()));
        services.AddProblemDetails();
        var provider = services.BuildServiceProvider();
        context = new DefaultHttpContext
        {
            RequestServices = provider,
            TraceIdentifier = "trace-296",
        };
        context.Response.Body = new MemoryStream();
        return new ValidationExceptionHandler(provider.GetRequiredService<IProblemDetailsService>());
    }

    private static async Task<JsonDocument> ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
