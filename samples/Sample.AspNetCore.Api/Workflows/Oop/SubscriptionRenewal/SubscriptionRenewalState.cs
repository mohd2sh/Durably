namespace Sample.AspNetCore.Api.Workflows.Oop.SubscriptionRenewal;

public sealed class SubscriptionRenewalState
{
    public string SubscriptionId { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public decimal QuotedAmount { get; set; }

    public bool Charged { get; set; }

    public bool ReceiptSent { get; set; }

    public string? Note { get; set; }
}
