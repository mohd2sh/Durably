namespace Durably.E2E.Tests.Models;

public sealed class OrderState
{
    public string? Report { get; set; }

    public bool EmailSent { get; set; }

    public bool Finalized { get; set; }
}
