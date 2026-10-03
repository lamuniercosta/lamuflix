using System;
using System.IO.Abstractions;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LamuFlix.Api;

internal static class HandlerRegistration
{
    internal static IServiceCollection AddLamuFlixHandlers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IFileSystem, FileSystem>();
        services.AddScoped<IMediaLibraryScanner, DirectoryMediaLibraryScanner>();

        return services;
    }
}
