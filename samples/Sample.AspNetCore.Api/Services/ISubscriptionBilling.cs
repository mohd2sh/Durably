namespace Sample.AspNetCore.Api.Services;

public interface ISubscriptionBilling
{
    Task<decimal> QuoteRenewalAsync(string subscriptionId, CancellationToken cancellationToken);

    Task ChargeRenewalAsync(string subscriptionId, decimal amount, string idempotencyKey, CancellationToken cancellationToken);
}
