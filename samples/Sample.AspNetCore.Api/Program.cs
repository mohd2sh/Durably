using Durably;
using Sample.AspNetCore.Api.Registration;
using Sample.AspNetCore.Api.Traceability;
using Sample.AspNetCore.Api.Workflows.Fluent.InvoiceReminder;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var sqlConnection =
    builder.Configuration.GetConnectionString("durable")
    ?? builder.Configuration.GetConnectionString("Durable");

var store = builder.Configuration["Durably:Store"];
if (string.IsNullOrWhiteSpace(store))
{
    store = string.IsNullOrWhiteSpace(sqlConnection) ? "InMemory" : "SqlServer";
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSampleApplication();
builder.Services.AddSingleton<ITraceRedactor, SampleTraceRedactor>();

var durably = builder.Services
    .AddDurably(o =>
    {
        o.DefaultRetry = RetryPolicy.Exponential(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5));
        o.DefaultStepTimeout = TimeSpan.FromSeconds(30);
    })
    .ConfigureWorker(o =>
    {
        o.Enabled = true;
        o.PollInterval = TimeSpan.FromMilliseconds(100);
        o.BatchSize = 16;
        o.LeaseDuration = TimeSpan.FromSeconds(30);
        o.RunnerId = "sample-api";
        o.MaxDegreeOfParallelism = 4;
    });

if (string.Equals(store, "SqlServer", StringComparison.OrdinalIgnoreCase))
{
    if (string.IsNullOrWhiteSpace(sqlConnection))
    {
        throw new InvalidOperationException(
            "Durably:Store=SqlServer requires ConnectionStrings:durable (Aspire) or ConnectionStrings:Durable.");
    }

    durably.UseSqlServer(sqlConnection, o => o.AutoMigrate = true);
}
else
{
    durably.UseInMemoryStore();
}

// Fluent/lambda sample (Workflows/Fluent) — registered explicitly via AddFlow.
var invoiceReminder = InvoiceReminderFlowDefinition.Build();
builder.Services.AddSingleton(invoiceReminder);

durably
    .AddTraceability(t =>
    {
        t.CaptureInputOutput = true;
        t.CaptureExceptions = true;
        t.FlushInterval = TimeSpan.FromSeconds(1);
    })
    .AddFlowsFromAssembly(typeof(Program).Assembly) // OOP samples under Workflows/Oop
    .AddFlow(invoiceReminder);

builder.Services.AddDurablyUI();

var app = builder.Build();

app.Logger.LogInformation("Durably sample store: {Store}", store);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDurablyUI("/durable");
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
