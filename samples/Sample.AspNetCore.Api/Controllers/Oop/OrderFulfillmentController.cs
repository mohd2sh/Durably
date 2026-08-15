using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Sample.AspNetCore.Api.Infrastructure;
using Sample.AspNetCore.Api.Models;
using Sample.AspNetCore.Api.Workflows.Oop.OrderFulfillment;

namespace Sample.AspNetCore.Api.Controllers.Oop;

[ApiController]
[Route("api/order-fulfillment")]
public sealed class OrderFulfillmentController : ControllerBase
{
    private readonly IFlowEngine _engine;

    public OrderFulfillmentController(IFlowEngine engine)
    {
        _engine = engine;
    }

    [HttpPost("{id}")]
    public async Task<IActionResult> Start(string id, [FromBody] OrderDto order, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Order id is required.");
        }

        order.Id = id;
        if (string.IsNullOrWhiteSpace(order.Channel))
        {
            order.Channel = "standard";
        }

        var state = new OrderFulfillmentState { Order = order };
        var options = new FlowStartOptions
        {
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = id,
                ["channel"] = order.Channel,
                ["total"] = order.Total.ToString("F2", CultureInfo.InvariantCulture),
                ["workflow"] = "OrderFulfillment"
            }
        };

        var result = await _engine.StartAsync<OrderFulfillmentFlow, OrderFulfillmentState>(id, state, options, cancellationToken);
        return FlowStartResults.ToActionResult(this, result);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Order id is required.");
        }

        var status = await _engine.GetStatusAsync<OrderFulfillmentFlow>(id, cancellationToken);
        return status is null ? NotFound() : Ok(status);
    }
}
