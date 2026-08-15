using Microsoft.AspNetCore.Mvc;
using Sample.AspNetCore.Api.Infrastructure;
using Sample.AspNetCore.Api.Models;
using Sample.AspNetCore.Api.Services;
using Sample.AspNetCore.Api.Workflows.Oop.OrderFinalize;

namespace Sample.AspNetCore.Api.Controllers.Oop;

[ApiController]
[Route("api/order-finalize")]
public sealed class OrderFinalizeController : ControllerBase
{
    private readonly IFlowEngine _engine;

    public OrderFinalizeController(IFlowEngine engine)
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
        var state = new OrderFinalizeState { Order = order };
        var options = new FlowStartOptions
        {
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = id,
                ["customerEmail"] = order.CustomerEmail,
                ["workflow"] = "OrderFinalize"
            }
        };

        var result = await _engine.StartAsync<OrderFinalizeFlow, OrderFinalizeState>(id, state, options, cancellationToken);
        return FlowStartResults.ToActionResult(this, result);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(string id, [FromQuery] string? runId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Order id is required.");
        }

        ExecutionStatusInfo? status = string.IsNullOrWhiteSpace(runId)
            ? await _engine.GetStatusAsync<OrderFinalizeFlow>(id, cancellationToken)
            : await _engine.GetStatusAsync(
                typeof(OrderFinalizeFlow).FullName ?? nameof(OrderFinalizeFlow),
                id,
                runId,
                cancellationToken);

        return status is null ? NotFound() : Ok(status);
    }

    [HttpPost("{id}/simulate-email-failure")]
    public IActionResult SimulateEmailFailure(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Order id is required.");
        }

        EmailService.SimulateFailureFor(id);
        return Ok(new { message = $"Next OrderFinalize for '{id}' will fail once at send-email, then resume on retry." });
    }
}
