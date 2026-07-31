using Durably;
using Sample.AspNetCore.Api.Models;

namespace Sample.AspNetCore.Api.Steps;

public sealed class ReserveStandardStep : IStep<OrderFulfillmentState>
{
    private readonly ILogger<ReserveStandardStep> _logger;

    public ReserveStandardStep(ILogger<ReserveStandardStep> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(OrderFulfillmentState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Reservation = "standard";
        _logger.LogInformation("Reserved standard capacity for order {OrderId}", state.Order.Id);

        await Task.Delay(TimeSpan.FromMinutes(2));

        //return Task.CompletedTask;
    }
}
