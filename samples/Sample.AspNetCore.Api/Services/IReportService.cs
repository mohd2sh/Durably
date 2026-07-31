namespace Sample.AspNetCore.Api.Services;

public interface IReportService
{
    Task<string> GenerateAsync(string orderId, CancellationToken cancellationToken);
}
