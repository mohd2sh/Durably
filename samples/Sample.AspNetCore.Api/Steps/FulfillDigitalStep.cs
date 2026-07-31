using Durably;
using Sample.AspNetCore.Api.Models;

namespace Sample.AspNetCore.Api.Steps;

public sealed class FulfillDigitalStep : IStep<OrderFulfillmentState>
{
    private readonly ILogger<FulfillDigitalStep> _logger;

    public FulfillDigitalStep(ILogger<FulfillDigitalStep> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(OrderFulfillmentState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Reservation = "digital";
        _logger.LogInformation("Queued digital delivery for order {OrderId}", state.Order.Id);
        return Task.CompletedTask;
    }
}
