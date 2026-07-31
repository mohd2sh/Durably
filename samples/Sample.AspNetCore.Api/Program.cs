using Durably;
using Sample.AspNetCore.Api.Registration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString =
    builder.Configuration.GetConnectionString("durable")
    ?? builder.Configuration.GetConnectionString("Durable")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=DurablySample_Api;Trusted_Connection=True;TrustServerCertificate=True";

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSampleApplication();

builder.Services
    .AddDurably(o =>
    {
        o.DefaultRetry = RetryPolicy.Exponential(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5));
    })
    .UseSqlServer(connectionString, o => o.AutoMigrate = true)
    .AddTraceability(t => t.FlushInterval = TimeSpan.FromSeconds(1))
    .AddFlowsFromAssembly(typeof(Program).Assembly);

builder.Services.AddDurablyUI();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDurablyUI("/durable");
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
