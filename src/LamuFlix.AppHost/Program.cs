using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "0.8.7-pg17")
    .WithImageSHA256("ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d");

var database = postgres.AddDatabase(name: "DefaultConnection", databaseName: "lamuflix");

builder.AddProject<Projects.LamuFlix_Api>("api")
    .WithHttpEndpoint(name: "http")
    .WithReference(database)
    .WaitFor(postgres)
    .WaitFor(database)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Database__ApplyMigrationsOnStartup", "true");

builder.Build().Run();
