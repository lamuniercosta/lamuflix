using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "0.8.7-pg17")
    .WithImageSHA256("ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d");

var database = postgres.AddDatabase(name: "DefaultConnection", databaseName: "lamuflix");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithImage("rabbitmq", "4-alpine");

builder.AddProject<Projects.LamuFlix_Api>("api")
    .WithHttpEndpoint(name: "http")
    .WithHttpHealthCheck(path: "/health/ready", endpointName: "http")
    .WithReference(database)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(database)
    .WaitFor(rabbitmq)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Database__ApplyMigrationsOnStartup", "true")
    .WithEnvironment("RabbitMq__HostName", rabbitmq.Resource.PrimaryEndpoint.Property(EndpointProperty.Host))
    .WithEnvironment("RabbitMq__Port", rabbitmq.Resource.PrimaryEndpoint.Property(EndpointProperty.Port))
    .WithEnvironment("RabbitMq__UserName", rabbitmq.Resource.UserNameReference)
    .WithEnvironment("RabbitMq__Password", rabbitmq.Resource.PasswordParameter)
    .WithEnvironment("Enrichment__ClaimLease", "00:00:30")
    .WithEnvironment("Enrichment__SweepInterval", "00:00:10")
    .WithEnvironment("RabbitMq__RetryDelay", "00:01:00");

builder.Build().Run();
