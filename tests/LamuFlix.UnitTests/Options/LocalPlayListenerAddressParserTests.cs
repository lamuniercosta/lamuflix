using System.Net;
using LamuFlix.Api;

namespace LamuFlix.UnitTests.Options;

public sealed class LocalPlayListenerAddressParserTests
{
    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("http://localhost")]
    [InlineData("https://localhost:5001")]
    [InlineData("http://LocalHost:5000")]
    [InlineData("http://127.0.0.1:5000")]
    [InlineData("http://127.1.2.3:5000")]
    [InlineData("http://127.255.255.254")]
    [InlineData("http://[::1]:5000")]
    [InlineData("http://[::ffff:127.0.0.1]:5000")]
    public void Classify_ProvenLoopbackForm_AcceptsAsProvenLoopback(string address)
    {
        // arrange
        // act
        var classification = LocalPlayListenerAddressParser.Classify(address);

        // assert
        classification.Kind.ShouldBe(LocalPlayListenerKind.ProvenLoopback);
        classification.IsProvenLoopback.ShouldBeTrue();
    }

    [Theory]
    [InlineData("http://0.0.0.0:5000")]
    [InlineData("http://[::]:5000")]
    [InlineData("http://*:5000")]
    [InlineData("http://+:5000")]
    public void Classify_WildcardForm_RejectsAsWildcard(string address)
    {
        // arrange
        // act
        var classification = LocalPlayListenerAddressParser.Classify(address);

        // assert
        classification.Kind.ShouldBe(LocalPlayListenerKind.Wildcard);
        classification.IsProvenLoopback.ShouldBeFalse();
    }

    [Theory]
    [InlineData("http://10.0.0.5:5000")]
    [InlineData("http://192.168.1.20")]
    [InlineData("https://203.0.113.9:5001")]
    [InlineData("http://[2001:db8::1]:5000")]
    [InlineData("http://[::ffff:10.0.0.5]:5000")]
    public void Classify_NonLoopbackLiteral_RejectsAsNonLoopbackLiteral(string address)
    {
        // arrange
        // act
        var classification = LocalPlayListenerAddressParser.Classify(address);

        // assert
        classification.Kind.ShouldBe(LocalPlayListenerKind.NonLoopbackLiteral);
        classification.IsProvenLoopback.ShouldBeFalse();
    }

    [Theory]
    [InlineData("http://devbox:5000")]
    [InlineData("http://devbox.local:5000")]
    [InlineData("https://lamuflix.internal")]
    public void Classify_HostnameOtherThanLocalhost_RejectsAsHostname(string address)
    {
        // arrange
        // act
        var classification = LocalPlayListenerAddressParser.Classify(address);

        // assert
        classification.Kind.ShouldBe(LocalPlayListenerKind.Hostname);
        classification.IsProvenLoopback.ShouldBeFalse();
    }

    [Theory]
    [InlineData("http://unix:/var/run/lamuflix.sock")]
    [InlineData("unix:/var/run/lamuflix.sock")]
    public void Classify_UnixSocketEndpoint_RejectsAsUnixSocket(string address)
    {
        // arrange
        // act
        var classification = LocalPlayListenerAddressParser.Classify(address);

        // assert
        classification.Kind.ShouldBe(LocalPlayListenerKind.UnixSocket);
        classification.IsProvenLoopback.ShouldBeFalse();
    }

    [Theory]
    [InlineData("http://pipe:/lamuflix")]
    [InlineData("http://pipe:lamuflix")]
    public void Classify_NamedPipeEndpoint_RejectsAsNamedPipe(string address)
    {
        // arrange
        // act
        var classification = LocalPlayListenerAddressParser.Classify(address);

        // assert
        classification.Kind.ShouldBe(LocalPlayListenerKind.NamedPipe);
        classification.IsProvenLoopback.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("localhost:5000")]
    [InlineData("not-a-url")]
    [InlineData("ftp://localhost:5000")]
    [InlineData("http://")]
    [InlineData("http://[::1:5000")]
    public void Classify_UnknownOrUnprovableForm_RejectsAsUnprovable(string? address)
    {
        // arrange
        // act
        var classification = LocalPlayListenerAddressParser.Classify(address);

        // assert
        classification.Kind.ShouldBe(LocalPlayListenerKind.Unprovable);
        classification.IsProvenLoopback.ShouldBeFalse();
    }

    [Fact]
    public void Split_MultipleSemicolonSeparatedAddresses_ReturnsEveryTrimmedEntry()
    {
        // arrange
        const string addressList = "http://127.0.0.1:5000 ; http://[::1]:5001;";

        // act
        var addresses = LocalPlayListenerAddressParser.Split(addressList);

        // assert
        addresses.ShouldBe(["http://127.0.0.1:5000", "http://[::1]:5001"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Split_EmptyAddressList_ReturnsNoAddresses(string? addressList)
    {
        // arrange
        // act
        var addresses = LocalPlayListenerAddressParser.Split(addressList);

        // assert
        addresses.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("::ffff:127.0.0.1", "127.0.0.1")]
    [InlineData("::ffff:127.0.0.53", "127.0.0.53")]
    public void Normalize_MappedIPv4Address_MapsBackToIPv4(string mapped, string expected)
    {
        // arrange
        var address = IPAddress.Parse(mapped);

        // act
        var normalized = LocalPlayListenerAddressParser.Normalize(address);

        // assert
        normalized.ShouldBe(IPAddress.Parse(expected));
        normalized.AddressFamily.ShouldBe(System.Net.Sockets.AddressFamily.InterNetwork);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("127.42.0.9")]
    public void IsProvenLoopback_LoopbackAddress_ReturnsTrue(string address)
    {
        // arrange
        // act
        var proven = LocalPlayListenerAddressParser.IsProvenLoopback(IPAddress.Parse(address));

        // assert
        proven.ShouldBeTrue();
    }

    [Theory]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("127.0.0.1")]
    public void IsProvenLoopback_MappedLoopbackAddress_ReturnsTrue(string address)
    {
        // arrange
        // act
        var proven = LocalPlayListenerAddressParser.IsProvenLoopback(IPAddress.Parse(address));

        // assert
        proven.ShouldBeTrue();
    }

    [Theory]
    [InlineData("10.0.0.5")]
    [InlineData("::ffff:10.0.0.5")]
    [InlineData("0.0.0.0")]
    [InlineData("2001:db8::1")]
    public void IsProvenLoopback_NonLoopbackAddress_ReturnsFalse(string address)
    {
        // arrange
        // act
        var proven = LocalPlayListenerAddressParser.IsProvenLoopback(IPAddress.Parse(address));

        // assert
        proven.ShouldBeFalse();
    }
}
