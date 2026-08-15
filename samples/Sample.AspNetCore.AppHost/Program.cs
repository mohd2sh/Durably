var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithDataVolume("durably-sql-data")
    .AddDatabase("durable");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("durably-pg-data")
    .AddDatabase("worker");

builder.AddProject<Projects.Sample_AspNetCore_Api>("api")
    .WithReference(sql);

builder.AddProject<Projects.Sample_Worker>("worker")
    .WithReference(postgres);

builder.Build().Run();
