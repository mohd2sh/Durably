using System.Collections.Concurrent;

namespace Sample.Worker.Services;

public sealed class EmailService : IEmailService
{
    private static readonly ConcurrentDictionary<string, byte> FailOnce = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, byte> SentKeys = new(StringComparer.Ordinal);

    public static void SimulateFailureFor(string orderId) => FailOnce[orderId] = 0;

    public Task SendAsync(string orderId, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(idempotencyKey) && SentKeys.ContainsKey(idempotencyKey))
        {
            return Task.CompletedTask;
        }

        if (FailOnce.TryRemove(orderId, out _))
        {
            throw new InvalidOperationException("SMTP server unavailable.");
        }

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            SentKeys[idempotencyKey] = 0;
        }

        return Task.CompletedTask;
    }
}
