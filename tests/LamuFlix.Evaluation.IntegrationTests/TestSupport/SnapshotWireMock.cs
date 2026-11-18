using WireMock.Server;
using WireMock.Settings;

namespace LamuFlix.Evaluation.IntegrationTests.TestSupport;

/// <summary>
/// Starts WireMock stubs on the loopback interface. A bare <c>WireMockServer.Start()</c> binds a
/// wildcard prefix (all interfaces), which makes Windows Firewall raise its network prompt for the
/// test host, so the stub binds 127.0.0.1 only.
/// </summary>
internal static class SnapshotWireMock
{
    public static WireMockServer Start() => WireMockServer.Start(new WireMockServerSettings
    {
        Urls = ["http://127.0.0.1:0"],
    });
}
