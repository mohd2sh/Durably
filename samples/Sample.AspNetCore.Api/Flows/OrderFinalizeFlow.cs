using Durably;
using Sample.AspNetCore.Api.Models;
using Sample.AspNetCore.Api.Steps;

namespace Sample.AspNetCore.Api.Flows;

public sealed class OrderFinalizeFlow : IFlow<OrderFinalizeState>
{
    public void Build(IFlowBuilder<OrderFinalizeState> builder) => builder
        .Step<GenerateReportStep>()
        .Step<SendEmailStep>(configure: o => o.Retry(RetryPolicy.Fixed(3, TimeSpan.FromSeconds(2))))
        .Step<FinalizeOrderStep>();
}
