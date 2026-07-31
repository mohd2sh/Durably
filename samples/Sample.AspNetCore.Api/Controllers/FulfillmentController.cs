using Durably;
using Microsoft.AspNetCore.Mvc;
using Sample.AspNetCore.Api.Flows;
using Sample.AspNetCore.Api.Models;

namespace Sample.AspNetCore.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class FulfillmentController : ControllerBase
{
    private readonly IFlowEngine _engine;

    public FulfillmentController(IFlowEngine engine)
    {
        _engine = engine;
    }

    [HttpPost("{id}/fulfill")]
    public async Task<IActionResult> Fulfill(string id, [FromBody] OrderDto order, CancellationToken cancellationToken)
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
                ["total"] = order.Total.ToString("F2")
            }
        };

        await _engine.StartAsync<OrderFulfillmentFlow, OrderFulfillmentState>(id, state, options, cancellationToken);

        return Accepted(new { status = "pending", orderId = id, flow = "fulfill" });
    }

    [HttpGet("{id}/fulfill/status")]
    public async Task<IActionResult> GetStatus(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Order id is required.");
        }

        var status = await _engine.GetStatusAsync<OrderFulfillmentFlow>(id, cancellationToken);
        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
    }
}
