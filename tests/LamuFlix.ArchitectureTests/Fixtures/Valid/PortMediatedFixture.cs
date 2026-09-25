using System;
using LamuFlix.ArchitectureTests.Fixtures.Valid.Ports;

// ReSharper disable once CheckNamespace
namespace LamuFlix.ArchitectureTests.Fixtures.Valid.Features.MediaLibrary;

public sealed class PortMediatedFixture
{
    public PortMediatedFixture(IMediaPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        Port = port;
    }

    public IMediaPort Port { get; }
}
