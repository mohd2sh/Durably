namespace Sample.Worker.Services;

public sealed class ReportService : IReportService
{
    public Task<string> GenerateAsync(string orderId, CancellationToken cancellationToken)
        => Task.FromResult($"Report for {orderId}");
}
