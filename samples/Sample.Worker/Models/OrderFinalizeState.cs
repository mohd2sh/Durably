namespace Sample.Worker.Models;

public sealed class OrderFinalizeState
{
    public string OrderId { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string? Report { get; set; }

    public bool EmailSent { get; set; }

    public bool Finalized { get; set; }
}
