namespace Durably.Core.UnitTests;

public sealed class GenerateReport : IStep<OrderState>
{
    public Task ExecuteAsync(OrderState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Report = "report";
        return Task.CompletedTask;
    }
}
