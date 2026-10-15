using WireMock.Server;
using WireMock.Settings;

namespace LamuFlix.IntegrationTests;

/// <summary>
/// Starts WireMock stubs on the loopback interface. A bare <c>WireMockServer.Start()</c> binds a wildcard
/// prefix (all interfaces), which makes Windows Firewall raise its public/private network prompt for the test host.
/// </summary>
public static class LoopbackWireMock
{
    public static WireMockServer Start() => WireMockServer.Start(new WireMockServerSettings
    {
        Urls = ["http://127.0.0.1:0"],
    });
}
