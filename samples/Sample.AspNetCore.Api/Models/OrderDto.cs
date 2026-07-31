namespace Sample.AspNetCore.Api.Models;

public sealed class OrderDto
{
    public string Id { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public string Channel { get; set; } = "standard";
}
