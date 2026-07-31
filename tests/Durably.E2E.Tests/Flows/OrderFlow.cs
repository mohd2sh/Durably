using Durably.E2E.Tests.Models;

namespace Durably.E2E.Tests.Flows;

public sealed class OrderFlow : IFlow<OrderState>
{
    public void Build(IFlowBuilder<OrderState> builder) => builder
        .Step<GenerateReportStep>()
        .Step<SendEmailStep>()
        .Step<FinalizeStep>();
}

public sealed class GenerateReportStep : IStep<OrderState>
{
    public Task ExecuteAsync(OrderState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Report = "report";
        return Task.CompletedTask;
    }
}

public sealed class SendEmailStep : IStep<OrderState>
{
    public Task ExecuteAsync(OrderState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.EmailSent = true;
        return Task.CompletedTask;
    }
}

public sealed class FinalizeStep : IStep<OrderState>
{
    public Task ExecuteAsync(OrderState state, IStepContext context, CancellationToken cancellationToken)
    {
        state.Finalized = true;
        return Task.CompletedTask;
    }
}
