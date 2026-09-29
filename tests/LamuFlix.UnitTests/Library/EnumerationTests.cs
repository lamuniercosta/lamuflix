using System.Text.Json;
using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;

namespace LamuFlix.UnitTests.Library;

public sealed class EnumerationTests
{
    [Fact]
    public void MovieSort_TryFromName_ExactNameSucceeds()
    {
        MovieSort.TryFromName("Title", false, out var sort).ShouldBeTrue();
        sort.ShouldBe(MovieSort.Title);
    }

    [Theory]
    [InlineData("InvalidSort")]
    [InlineData("title")]
    public void MovieSort_TryFromName_NonExactOrUnknownFails(string name)
    {
        MovieSort.TryFromName(name, false, out _).ShouldBeFalse();
    }

    [Fact]
    public void SortDirection_TryFromName_ExactNameSucceeds()
    {
        SortDirection.TryFromName("Ascending", false, out var direction).ShouldBeTrue();
        direction.ShouldBe(SortDirection.Ascending);
    }

    [Theory]
    [InlineData("InvalidDirection")]
    [InlineData("ascending")]
    public void SortDirection_TryFromName_NonExactOrUnknownFails(string name)
    {
        SortDirection.TryFromName(name, false, out _).ShouldBeFalse();
    }

    [Fact]
    public void Json_RoundTripsEnumerationNames()
    {
        RoundTrip(MovieSort.Title, new SmartEnumNameConverter<MovieSort, int>()).ShouldBe("\"Title\"");
        RoundTrip(SortDirection.Ascending, new SmartEnumNameConverter<SortDirection, int>()).ShouldBe("\"Ascending\"");
        RoundTrip(EnrichmentStatus.Failed, new SmartEnumNameConverter<EnrichmentStatus, int>()).ShouldBe("\"Failed\"");
    }

    private static string RoundTrip<TEnum>(TEnum value, JsonConverter<TEnum> converter)
        where TEnum : Ardalis.SmartEnum.SmartEnum<TEnum, int>
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(converter);
        var json = JsonSerializer.Serialize(value, options);
        var parsed = JsonSerializer.Deserialize<TEnum>(json, options);
        parsed.ShouldBe(value);
        return json;
    }
}
