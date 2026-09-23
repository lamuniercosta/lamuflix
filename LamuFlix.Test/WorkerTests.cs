using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LamuFlix.Test
{
    [TestClass]
    public sealed class WorkerTests
    {
        [TestMethod]
        public void QueueWorker_ImplementsBackgroundServiceAndHostedService()
        {
            var config = new ConfigurationBuilder().Build();
            var logger = NullLogger<QueueWorker>.Instance;

            using var worker = new QueueWorker(logger, config);

            Assert.IsInstanceOfType(worker, typeof(BackgroundService));
            Assert.IsInstanceOfType(worker, typeof(IHostedService));
        }

        [TestMethod]
        // ReSharper disable NullableWarningSuppressionIsUsed
        // Deliberate null asserting ArgumentNullException
        public void QueueWorker_Constructor_ThrowsOnNullLogger()
        {
            var config = new ConfigurationBuilder().Build();

            Assert.ThrowsException<ArgumentNullException>(() => new QueueWorker(null!, config));
        }
        // ReSharper restore NullableWarningSuppressionIsUsed

        [TestMethod]
        // ReSharper disable NullableWarningSuppressionIsUsed
        // Deliberate null asserting ArgumentNullException
        public void QueueWorker_Constructor_ThrowsOnNullConfiguration()
        {
            var logger = NullLogger<QueueWorker>.Instance;

            Assert.ThrowsException<ArgumentNullException>(() => new QueueWorker(logger, null!));
        }
        // ReSharper restore NullableWarningSuppressionIsUsed

        [TestMethod]
        public void CreateHostBuilder_RegistersQueueWorkerAsHostedService()
        {
            var hostBuilder = Program.CreateHostBuilder(Array.Empty<string>());
            using var host = hostBuilder.Build();

            var hostedServices = host.Services.GetServices<IHostedService>();
            var workerService = hostedServices.FirstOrDefault(s => s is QueueWorker);

            Assert.IsNotNull(workerService);
        }

        [TestMethod]
        public async Task QueueWorker_StartAsync_WhenCancelled_TerminatesCleanly()
        {
            var inMemoryConfig = new Dictionary<string, string?>
            {
                ["RabbitMQ:HostName"] = "nonexistent.local",
                ["RabbitMQ:QueueName"] = "test_queue"
            };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfig)
                .Build();
            var logger = NullLogger<QueueWorker>.Instance;

            using var worker = new QueueWorker(logger, config);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await worker.StartAsync(cts.Token);
            await worker.StopAsync(CancellationToken.None);
        }
    }
}
