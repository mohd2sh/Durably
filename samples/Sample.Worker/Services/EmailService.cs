namespace Sample.Worker.Services;

public sealed class EmailService : IEmailService
{
    private static readonly HashSet<string> SimulatedFailures = new(StringComparer.OrdinalIgnoreCase) { "order-2" };

    public static void SimulateFailureFor(string orderId) => SimulatedFailures.Add(orderId);

    public static void ClearSimulatedFailures() => SimulatedFailures.Clear();

    public Task SendAsync(string orderId, CancellationToken cancellationToken)
    {
        if (!SimulatedFailures.Contains(orderId))
        {
            return Task.CompletedTask;
        }

        SimulatedFailures.Remove(orderId);
        throw new InvalidOperationException("SMTP server unavailable.");
    }
}
