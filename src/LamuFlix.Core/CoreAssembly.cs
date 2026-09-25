using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LamuFlix.Core;

public static class CoreAssembly
{
    public static Assembly Instance => typeof(CoreAssembly).Assembly;

    public static ILogger Logger => NullLogger.Instance;

    public static ImmutableArray<string> Categories => ImmutableArray<string>.Empty;
}
