using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LamuFlix.Data;
using LamuFlix.Worker.Services;

namespace LamuFlix.Worker;

public static class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(config => config.AddUserSecrets(typeof(Program).Assembly, optional: true))
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;
                var connectionString = configuration.GetConnectionString("DefaultConnection")
                                       ?? throw new InvalidOperationException(
                                           "Connection string 'DefaultConnection' not configured. Set 'ConnectionStrings:DefaultConnection' via user secrets " +
                                           "or the ConnectionStrings__DefaultConnection environment variable.");
                services.AddDbContext<LamuFlixContext>(opts => opts.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 31)), s => s.MigrationsAssembly("LamuFlix.Data")));

                services.AddHttpClient<IMetadataProvider, OmdbMetadataProvider>();
                services.AddTransient<IEnrichmentJobProcessor, EnrichmentJobProcessor>();
                services.AddHostedService<QueueWorker>();
            });
}