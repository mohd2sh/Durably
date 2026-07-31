using Durably;
using Sample.AspNetCore.Api.Models;

namespace Sample.AspNetCore.Api.Steps;

public sealed class ReserveExpressStep : IStep<OrderFulfillmentState>
{
    private readonly ILogger<ReserveExpressStep> _logger;

    public ReserveExpressStep(ILogger<ReserveExpressStep> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(OrderFulfillmentState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Reservation = "express";
        _logger.LogInformation("Reserved express capacity for order {OrderId}", state.Order.Id);
        return Task.CompletedTask;
    }
}
