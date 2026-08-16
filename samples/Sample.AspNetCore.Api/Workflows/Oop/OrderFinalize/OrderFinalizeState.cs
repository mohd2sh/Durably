using Sample.AspNetCore.Api.Models;

namespace Sample.AspNetCore.Api.Workflows.Oop.OrderFinalize;

public sealed class OrderFinalizeState
{
    public OrderDto Order { get; set; } = new();

    public string? Report { get; set; }

    public bool EmailSent { get; set; }

    public bool Finalized { get; set; }
}
