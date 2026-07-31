using Durably;
using Microsoft.AspNetCore.Mvc;
using Sample.AspNetCore.Api.Flows;
using Sample.AspNetCore.Api.Models;
using Sample.AspNetCore.Api.Services;

namespace Sample.AspNetCore.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IFlowEngine _engine;

    public OrdersController(IFlowEngine engine)
    {
        _engine = engine;
    }

    [HttpPost("{id}/finalize")]
    public async Task<IActionResult> Finalize(string id, [FromBody] OrderDto order, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Order id is required.");
        }

        order.Id = id;
        var state = new OrderFinalizeState { Order = order };

        var options = new FlowStartOptions
        {
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = id,
                ["customerEmail"] = order.CustomerEmail
            }
        };

        await _engine.StartAsync<OrderFinalizeFlow, OrderFinalizeState>(id, state, options, cancellationToken);

        return Accepted(new { status = "pending", orderId = id });
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Order id is required.");
        }

        var status = await _engine.GetStatusAsync<OrderFinalizeFlow>(id, cancellationToken);
        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
    }

    [HttpPost("{id}/simulate-email-failure")]
    public IActionResult SimulateEmailFailure(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Order id is required.");
        }

        EmailService.SimulateFailureFor(id);
        return Ok(new { message = $"Next finalize for order '{id}' will fail at send-email, then resume on retry." });
    }
}
