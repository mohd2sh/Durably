using Sample.AspNetCore.Api.Services;

namespace Sample.AspNetCore.Api.Workflows.Oop.SubscriptionRenewal;

public sealed class ChargeRenewalStep : IStep<SubscriptionRenewalState>
{
    private readonly ISubscriptionBilling _billing;

    public ChargeRenewalStep(ISubscriptionBilling billing)
    {
        _billing = billing;
    }

    public async Task ExecuteAsync(SubscriptionRenewalState state, IStepContext context, CancellationToken cancellationToken)
    {
        await _billing.ChargeRenewalAsync(state.SubscriptionId, state.QuotedAmount, context.IdempotencyKey, cancellationToken);
        state.Charged = true;
    }
}
