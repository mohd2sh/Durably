using Durably;
using Sample.Worker.Models;
using Sample.Worker.Services;
using Sample.Worker.Steps;
using Sample.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

var postgresConnection =
    builder.Configuration.GetConnectionString("worker")
    ?? builder.Configuration.GetConnectionString("Durable");

var store = builder.Configuration["Durably:Store"];
if (string.IsNullOrWhiteSpace(store))
{
    store = string.IsNullOrWhiteSpace(postgresConnection) ? "InMemory" : "Postgres";
}

// Fluent registration contrast to the API sample's IFlow + AddFlowsFromAssembly style.
var orderFinalizeFlow = Flow.For<OrderFinalizeState>()
    .Step<GenerateReportStep>()
    .Step<SendEmailStep>(configure: o => o.Retry(RetryPolicy.Fixed(3, TimeSpan.FromSeconds(1))))
    .Step<FinalizeOrderStep>();

builder.Services
    .AddSingleton<IReportService, ReportService>()
    .AddSingleton<IEmailService, EmailService>()
    .AddSingleton<IOrderService, OrderService>()
    .AddTransient<GenerateReportStep>()
    .AddTransient<SendEmailStep>()
    .AddTransient<FinalizeOrderStep>()
    .AddSingleton<IFlowBuilder<OrderFinalizeState>>(orderFinalizeFlow);

var durably = builder.Services
    .AddDurably()
    .ConfigureWorker(o =>
    {
        o.Enabled = true;
        o.PollInterval = TimeSpan.FromMilliseconds(200);
        o.BatchSize = 8;
        o.RunnerId = "sample-worker";
    });

if (string.Equals(store, "Postgres", StringComparison.OrdinalIgnoreCase))
{
    if (string.IsNullOrWhiteSpace(postgresConnection))
    {
        throw new InvalidOperationException(
            "Durably:Store=Postgres requires ConnectionStrings:worker (Aspire) or ConnectionStrings:Durable.");
    }

    durably.UsePostgres(postgresConnection, o => o.AutoMigrate = true);
}
else
{
    durably.UseInMemoryStore();
}

durably
    .AddFlow(orderFinalizeFlow)
    .AddTraceability(t =>
    {
        t.CaptureInputOutput = true;
        t.CaptureExceptions = true;
        t.FlushInterval = TimeSpan.FromSeconds(1);
    });

builder.Services.AddHostedService<OrderFinalizeWorker>();

var host = builder.Build();
host.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("Sample.Worker")
    .LogInformation("Durably worker sample store: {Store}", store);

host.Run();
