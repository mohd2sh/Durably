namespace Sample.AspNetCore.Api.Services;

public interface IOrderService
{
    Task FinalizeAsync(string orderId, CancellationToken cancellationToken);
}
