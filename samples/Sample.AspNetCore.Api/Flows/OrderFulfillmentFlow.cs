using Sample.AspNetCore.Api.Models;
using Sample.AspNetCore.Api.Steps;

namespace Sample.AspNetCore.Api.Flows;

public sealed class OrderFulfillmentFlow : IFlow<OrderFulfillmentState>
{
    public void Build(IFlowBuilder<OrderFulfillmentState> builder) => builder
        .Step<ValidateOrderStep>()
        .StepIf<FraudCheckStep>(s => s.Order.Total >= 500m)
        .Choose(s => s.Order.Channel)
            .When("express", b => b.Step<ReserveExpressStep>())
            .When("standard", b => b.Step<ReserveStandardStep>())
            .Otherwise(b => b.Step<FulfillDigitalStep>())
        .EndChoose()
        .Step<MarkFulfilledStep>()
        .OnSuccess(s => s.CompletionNote = "fulfilled")
        .OnFailure((s, ex) => s.FailureNote = ex?.Message);
}
