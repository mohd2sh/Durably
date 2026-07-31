namespace Sample.AspNetCore.Api.Services;

public sealed class ReportService : IReportService
{
    public Task<string> GenerateAsync(string orderId, CancellationToken cancellationToken)
    {
        var report = $"Report for order {orderId} generated at {DateTimeOffset.UtcNow:O}";
        return Task.FromResult(report);
    }
}
