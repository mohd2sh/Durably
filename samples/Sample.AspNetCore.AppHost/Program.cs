var builder = DistributedApplication.CreateBuilder(args);

var pg = builder.AddPostgres("postgres")
    .WithDataVolume("durably-pg-data");
var durable = pg.AddDatabase("durable");
var workerDb = pg.AddDatabase("worker");

builder.AddProject<Projects.Sample_AspNetCore_Api>("api", launchProfileName: null)
    .WithHttpEndpoint(name: "http")
    .WithHttpsEndpoint(name: "https")
    .WithExternalHttpEndpoints()
    .WithReference(durable)
    .WaitFor(pg)
    .WithEnvironment("Durably__Store", "Postgres")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

builder.AddProject<Projects.Sample_Worker>("sample-worker")
    .WithReference(workerDb)
    .WaitFor(pg)
    .WithEnvironment("Durably__Store", "Postgres")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development");

builder.Build().Run();
