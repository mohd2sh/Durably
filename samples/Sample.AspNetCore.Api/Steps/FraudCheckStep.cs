using Durably;
using Sample.AspNetCore.Api.Models;

namespace Sample.AspNetCore.Api.Steps;

public sealed class FraudCheckStep : IStep<OrderFulfillmentState>
{
    private readonly ILogger<FraudCheckStep> _logger;

    public FraudCheckStep(ILogger<FraudCheckStep> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(OrderFulfillmentState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.FraudChecked = true;
        _logger.LogInformation(
            "Fraud check passed for high-value order {OrderId} (Total={Total})",
            state.Order.Id,
            state.Order.Total);
        return Task.CompletedTask;
    }
}
