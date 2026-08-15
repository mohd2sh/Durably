namespace Sample.AspNetCore.Api.Workflows.Oop.SubscriptionRenewal;

public sealed class SubscriptionRenewalFlow : IFlow<SubscriptionRenewalState>
{
    public void Build(IFlowBuilder<SubscriptionRenewalState> builder) => builder
        // Lambda step for a simple quote; OOP step for the side-effecting charge.
        .Step("quote-renewal", async (state, ct) =>
        {
            // Resolved via ambient DI is not available in lambdas — quote is inlined for demo clarity.
            state.QuotedAmount = 29.99m;
            state.Note = $"Quoted {state.QuotedAmount:C} for {state.SubscriptionId}";
            await Task.CompletedTask;
        })
        .Step<ChargeRenewalStep>()
        .Step("send-receipt", async (state, ct) =>
        {
            state.ReceiptSent = true;
            state.Note = $"Receipt sent to {state.CustomerEmail}";
            await Task.CompletedTask;
        })
        .OnSuccess(s => s.Note = $"Renewed {s.SubscriptionId} for {s.QuotedAmount:C}");
}
