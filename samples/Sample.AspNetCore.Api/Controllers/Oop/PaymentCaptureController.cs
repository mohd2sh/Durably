using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Sample.AspNetCore.Api.Infrastructure;
using Sample.AspNetCore.Api.Services;
using Sample.AspNetCore.Api.Workflows.Oop.PaymentCapture;

namespace Sample.AspNetCore.Api.Controllers.Oop;

[ApiController]
[Route("api/payment-capture")]
public sealed class PaymentCaptureController : ControllerBase
{
    private readonly IFlowEngine _engine;

    public PaymentCaptureController(IFlowEngine engine)
    {
        _engine = engine;
    }

    public sealed class PaymentRequest
    {
        public decimal Amount { get; set; } = 10m;
    }

    [HttpPost("{id}")]
    public async Task<IActionResult> Start(string id, [FromBody] PaymentRequest? request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Payment id is required.");
        }

        var state = new PaymentCaptureState
        {
            PaymentId = id,
            Amount = request?.Amount > 0 ? request.Amount : 10m
        };

        var options = new FlowStartOptions
        {
            Metadata = new Dictionary<string, string>
            {
                ["paymentId"] = id,
                ["amount"] = state.Amount.ToString("F2", CultureInfo.InvariantCulture),
                ["workflow"] = "PaymentCapture"
            }
        };

        var result = await _engine.StartAsync<PaymentCaptureFlow, PaymentCaptureState>(id, state, options, cancellationToken);
        return FlowStartResults.ToActionResult(this, result);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(string id, CancellationToken cancellationToken)
    {
        var status = await _engine.GetStatusAsync<PaymentCaptureFlow>(id, cancellationToken);
        return status is null ? NotFound() : Ok(status);
    }

    [HttpPost("{id}/simulate-transient")]
    public IActionResult SimulateTransient(string id)
    {
        PaymentGateway.SimulateTransientFailure(id);
        return Ok(new { message = "Next capture will throw TransientPaymentException once (retried)." });
    }

    [HttpPost("{id}/simulate-permanent")]
    public IActionResult SimulatePermanent(string id)
    {
        PaymentGateway.SimulatePermanentFailure(id);
        return Ok(new { message = "Next capture will throw PermanentPaymentException (not retried)." });
    }

    [HttpPost("{id}/simulate-timeout")]
    public IActionResult SimulateTimeout(string id)
    {
        PaymentGateway.SimulateSlowCapture(id);
        return Ok(new { message = "Next capture will exceed the 2s step timeout." });
    }
}
