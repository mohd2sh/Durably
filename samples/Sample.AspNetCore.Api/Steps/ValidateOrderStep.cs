using Durably;
using Sample.AspNetCore.Api.Models;

namespace Sample.AspNetCore.Api.Steps;

public sealed class ValidateOrderStep : IStep<OrderFulfillmentState>
{
    private readonly ILogger<ValidateOrderStep> _logger;

    public ValidateOrderStep(ILogger<ValidateOrderStep> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(OrderFulfillmentState state, IStepContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state.Order.Id))
        {
            throw new InvalidOperationException("Order id is required.");
        }

        if (string.IsNullOrWhiteSpace(state.Order.CustomerEmail))
        {
            throw new InvalidOperationException("Customer email is required.");
        }

        state.Validated = true;
        _logger.LogInformation("Validated order {OrderId}", state.Order.Id);
        return Task.CompletedTask;
    }
}
