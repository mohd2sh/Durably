using System.Collections.Concurrent;

namespace Sample.AspNetCore.Api.Services;

public sealed class SubscriptionBilling : ISubscriptionBilling
{
    private static readonly ConcurrentDictionary<string, byte> Charged = new(StringComparer.Ordinal);

    public Task<decimal> QuoteRenewalAsync(string subscriptionId, CancellationToken cancellationToken)
        => Task.FromResult(29.99m);

    public Task ChargeRenewalAsync(string subscriptionId, decimal amount, string idempotencyKey, CancellationToken cancellationToken)
    {
        Charged[idempotencyKey] = 0;
        return Task.CompletedTask;
    }
}