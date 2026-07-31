namespace Sample.AspNetCore.Api.Models;

public sealed class OrderFulfillmentState
{
    public OrderDto Order { get; set; } = new();

    public bool Validated { get; set; }

    public bool FraudChecked { get; set; }

    public string? Reservation { get; set; }

    public bool Fulfilled { get; set; }

    public string? CompletionNote { get; set; }

    public string? FailureNote { get; set; }
}
