using WireMock.Server;
using WireMock.Settings;

namespace LamuFlix.Evaluation.IntegrationTests.TestSupport;

internal static class SnapshotWireMock
{
    public static WireMockServer Start() => WireMockServer.Start(new WireMockServerSettings
    {
        Urls = ["http://127.0.0.1:0"],
    });
}
