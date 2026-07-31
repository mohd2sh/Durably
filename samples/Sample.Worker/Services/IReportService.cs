namespace Sample.Worker.Services;

public interface IReportService
{
    Task<string> GenerateAsync(string orderId, CancellationToken cancellationToken);
}
