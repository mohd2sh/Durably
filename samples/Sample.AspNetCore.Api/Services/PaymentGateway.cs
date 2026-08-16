using System.Collections.Concurrent;

namespace Sample.AspNetCore.Api.Services;

public sealed class TransientPaymentException : Exception
{
    public TransientPaymentException(string message) : base(message)
    {
    }
}

public sealed class PermanentPaymentException : Exception
{
    public PermanentPaymentException(string message) : base(message)
    {
    }
}

public sealed class PaymentGateway : IPaymentGateway
{
    private static readonly ConcurrentDictionary<string, string> NextFault = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, byte> Captured = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> Settled = new(StringComparer.Ordinal);

    public static void SimulateTransientFailure(string paymentId) => NextFault[paymentId] = "transient";

    public static void SimulatePermanentFailure(string paymentId) => NextFault[paymentId] = "permanent";

    public static void SimulateSlowCapture(string paymentId) => NextFault[paymentId] = "slow";

    public async Task CaptureAsync(string paymentId, decimal amount, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (Captured.ContainsKey(idempotencyKey))
        {
            return;
        }

        if (NextFault.TryRemove(paymentId, out var fault))
        {
            if (fault == "transient")
            {
                throw new TransientPaymentException("Payment network timeout.");
            }

            if (fault == "permanent")
            {
                throw new PermanentPaymentException("Card declined.");
            }

            if (fault == "slow")
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        Captured[idempotencyKey] = 0;
    }

    public Task SettleAsync(string paymentId, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (Settled.ContainsKey(idempotencyKey))
        {
            return Task.CompletedTask;
        }

        Settled[idempotencyKey] = 0;
        return Task.CompletedTask;
    }
}
