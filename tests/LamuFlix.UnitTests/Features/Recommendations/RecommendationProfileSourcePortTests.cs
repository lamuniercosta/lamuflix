using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class RecommendationProfileSourcePortTests
{
    [Fact]
    public void Port_RetainsProfileInputsRetrieval()
    {
        // act
        var method = PortMethod("GetProfileInputsAsync");

        // assert
        method.ShouldNotBeNull();
    }

    [Fact]
    public void Port_AddsCancellationOnlyDerivedProfileRetrieval()
    {
        // act
        var method = PortMethod("GetTasteProfileAsync");

        // assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(Task<TasteProfile>));
        method.GetParameters().Select(parameter => parameter.ParameterType).ShouldBe([typeof(CancellationToken)]);
    }

    private static MethodInfo? PortMethod(string name) =>
        typeof(IRecommendationProfileSource).GetMethod(name);
}
