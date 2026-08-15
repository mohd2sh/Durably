namespace Sample.AspNetCore.Api.Workflows.Oop.PaymentCapture;

public sealed class PaymentCaptureState
{
    public string PaymentId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public bool Captured { get; set; }

    public bool Settled { get; set; }

    public int CaptureAttempts { get; set; }
}
