using System;

namespace LamuFlix.Tests.Common;

/// <summary>
/// One activated PostgreSQL container shared by every test host that only needs the Api to start:
/// the startup library path gate verifies the activated schema, so those hosts need real activated
/// tables rather than an unreachable placeholder connection string.
/// </summary>
public static class SharedPostgres
{
    private static readonly Lazy<string> Holder = new(Start, isThreadSafe: true);

    public static string ConnectionString => Holder.Value;

    private static string Start()
    {
        var fixture = new PostgresFixture();
        fixture.InitializeAsync().AsTask().GetAwaiter().GetResult();
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();
        return fixture.ConnectionString;
    }
}
