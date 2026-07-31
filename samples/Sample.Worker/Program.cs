using Durably;
using Sample.Worker.Models;
using Sample.Worker.Services;
using Sample.Worker.Steps;
using Sample.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Durable")
    ?? "Host=localhost;Port=5432;Database=durably_worker_sample;Username=postgres;Password=postgres";

var orderFinalizeFlow = Flow.For<OrderFinalizeState>()
    .Step<GenerateReportStep>()
    .Step<SendEmailStep>()
    .Step<FinalizeOrderStep>();

builder.Services
    .AddSingleton<IReportService, ReportService>()
    .AddSingleton<IEmailService, EmailService>()
    .AddSingleton<IOrderService, OrderService>()
    .AddTransient<GenerateReportStep>()
    .AddTransient<SendEmailStep>()
    .AddTransient<FinalizeOrderStep>()
    .AddSingleton<IFlowBuilder<OrderFinalizeState>>(orderFinalizeFlow);

builder.Services
    .AddDurably()
    .UsePostgres(connectionString, o => o.AutoMigrate = true)
    .AddFlow(orderFinalizeFlow)
    .AddTraceability(t => t.FlushInterval = TimeSpan.FromSeconds(1));

builder.Services.AddHostedService<OrderFinalizeWorker>();

builder.Build().Run();
