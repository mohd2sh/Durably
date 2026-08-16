using Durably;

namespace Sample.AspNetCore.Api.Workflows.Oop.OrderFinalize;

public sealed class OrderFinalizeFlow : IFlow<OrderFinalizeState>
{
    public void Build(IFlowBuilder<OrderFinalizeState> builder) => builder
        .Step<GenerateReportStep>()
        .Step<SendEmailStep>(configure: o => o.Retry(RetryPolicy.Fixed(3, TimeSpan.FromSeconds(2))))
        .Step<FinalizeOrderStep>();
}
