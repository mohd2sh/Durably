var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithDataVolume("durably-sql-data")
    .AddDatabase("durable");

builder.AddProject<Projects.Sample_AspNetCore_Api>("api")
    .WithReference(sql);

builder.Build().Run();
