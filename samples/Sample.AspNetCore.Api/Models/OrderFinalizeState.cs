namespace Sample.AspNetCore.Api.Models;

public sealed class OrderFinalizeState
{
    public OrderDto Order { get; set; } = new();

    public string? Report { get; set; }

    public bool EmailSent { get; set; }

    public bool Finalized { get; set; }
}
