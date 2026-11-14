using System.Diagnostics;
using LamuFlix.Api.ExceptionHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LamuFlix.UnitTests.ExceptionHandling;

public sealed class ProblemDetailsTraceIdTests
{
    private const string TraceId = "e9b1d5e37150b219e50208499f8f2349";

    [Fact]
    public void Apply_W3CActivity_WritesBareTraceIdNotTheTraceparent()
    {
        // arrange
        using var activity = new Activity("request");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.SetParentId($"00-{TraceId}-ee5989b59d892a88-01");
        activity.Start();
        var context = CreateContext();

        // act
        ProblemDetailsTraceId.Apply(context);

        // assert
        context.ProblemDetails.Extensions[ProblemDetailsTraceId.ExtensionName].ShouldBe(TraceId);
    }

    [Fact]
    public void Apply_NoActivity_FallsBackToTheHttpTraceIdentifier()
    {
        // arrange
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            var context = CreateContext();

            // act
            ProblemDetailsTraceId.Apply(context);

            // assert
            context.ProblemDetails.Extensions[ProblemDetailsTraceId.ExtensionName].ShouldBe("trace-296");
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    private static ProblemDetailsContext CreateContext() =>
        new()
        {
            HttpContext = new DefaultHttpContext { TraceIdentifier = "trace-296" },
            ProblemDetails = new ProblemDetails(),
        };
}
