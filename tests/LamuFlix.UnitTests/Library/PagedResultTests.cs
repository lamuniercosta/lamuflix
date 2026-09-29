using System;
using System.Collections.Immutable;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Library;

public sealed class PagedResultTests
{

    [Fact]
    public void Constructor_NonDefaultItemsAndSufficientTotal_SetsItemsAndTotalCount()
    {
        var items = ImmutableArray.Create("a");

        var result = new PagedResult<string>(items, 2);

        result.Items.ShouldBe(items);
        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public void Constructor_TotalCountEqualsItemsLength_IsAccepted()
    {
        var items = ImmutableArray.Create(1, 2);

        var result = new PagedResult<int>(items, items.Length);

        result.Items.ShouldBe(items);
        result.TotalCount.ShouldBe(items.Length);
    }

    [Fact]
    public void Constructor_DefaultItems_ThrowsArgumentException()
    {
        var exception = Should.Throw<ArgumentException>(() => new PagedResult<int>(default, 0));

        exception.Message.ShouldStartWith("Items must be initialized.");
        exception.ParamName.ShouldBe("items");
    }

    [Fact]
    public void Constructor_NegativeTotalCount_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => new PagedResult<int>(ImmutableArray<int>.Empty, -1));
    }

    [Fact]
    public void Constructor_TotalCountLessThanItemsLength_ThrowsArgumentOutOfRangeException()
    {
        var items = ImmutableArray.Create("a", "b");

        Should.Throw<ArgumentOutOfRangeException>(() => new PagedResult<string>(items, 1));
    }
}
