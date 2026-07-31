using Durably;
using Sample.AspNetCore.Api.Models;

namespace Sample.AspNetCore.Api.Steps;

public sealed class MarkFulfilledStep : IStep<OrderFulfillmentState>
{
    private readonly ILogger<MarkFulfilledStep> _logger;

    public MarkFulfilledStep(ILogger<MarkFulfilledStep> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(OrderFulfillmentState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Fulfilled = true;
        _logger.LogInformation(
            "Marked order {OrderId} fulfilled via {Reservation}",
            state.Order.Id,
            state.Reservation);
        return Task.CompletedTask;
    }
}
