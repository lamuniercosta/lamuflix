using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class InvalidTransitionExceptionSurvivorTests
{
    [Fact]
    public void Constructor_Parameterless_ExposesUnknownActionAndState()
    {
        // act
        var thrown = new InvalidTransitionException();

        // assert
        thrown.Action.ShouldBe("Unknown");
        thrown.State.ShouldBe("Unknown");
        thrown.Message.ShouldBe("Cannot Unknown from Unknown.");
    }

    [Fact]
    public void Constructor_MessageOnly_LeavesActionAndStateEmpty()
    {
        // act
        var thrown = new InvalidTransitionException("Custom failure.");

        // assert
        thrown.Message.ShouldBe("Custom failure.");
        thrown.Action.ShouldBe(string.Empty);
        thrown.State.ShouldBe(string.Empty);
    }

    [Fact]
    public void Constructor_MessageAndInnerException_LeavesActionAndStateEmpty()
    {
        // arrange
        var inner = new InvalidOperationException("Inner failure.");

        // act
        var thrown = new InvalidTransitionException("Custom failure.", inner);

        // assert
        thrown.Message.ShouldBe("Custom failure.");
        thrown.InnerException.ShouldBeSameAs(inner);
        thrown.Action.ShouldBe(string.Empty);
        thrown.State.ShouldBe(string.Empty);
    }

    [Fact]
    public void Constructor_ActionAndState_ExposesBothAndBuildsMessage()
    {
        // act
        var thrown = new InvalidTransitionException("Approve", "Closed");

        // assert
        thrown.Action.ShouldBe("Approve");
        thrown.State.ShouldBe("Closed");
        thrown.Message.ShouldBe("Cannot Approve from Closed.");
    }
}
