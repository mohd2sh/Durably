using Microsoft.AspNetCore.Mvc;
using Sample.AspNetCore.Api.Infrastructure;
using Sample.AspNetCore.Api.Workflows.Oop.SubscriptionRenewal;

namespace Sample.AspNetCore.Api.Controllers.Oop;

[ApiController]
[Route("api/subscription-renewal")]
public sealed class SubscriptionRenewalController : ControllerBase
{
    private readonly IFlowEngine _engine;

    public SubscriptionRenewalController(IFlowEngine engine)
    {
        _engine = engine;
    }

    public sealed class RenewalRequest
    {
        public string CustomerEmail { get; set; } = "subscriber@example.com";
    }

    /// <summary>
    /// Idempotent start: if a run is already open, returns Skipped with the existing run id
    /// (<see cref="OpenConflictPolicy.Skip"/>). After a terminal run, the same instance id starts a new run id.
    /// </summary>
    [HttpPost("{id}")]
    public async Task<IActionResult> Start(string id, [FromBody] RenewalRequest? request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Subscription id is required.");
        }

        var state = new SubscriptionRenewalState
        {
            SubscriptionId = id,
            CustomerEmail = request?.CustomerEmail ?? "subscriber@example.com"
        };

        var options = new FlowStartOptions
        {
            OpenConflict = OpenConflictPolicy.Skip,
            Metadata = new Dictionary<string, string>
            {
                ["subscriptionId"] = id,
                ["customerEmail"] = state.CustomerEmail,
                ["workflow"] = "SubscriptionRenewal"
            }
        };

        var result = await _engine.StartAsync<SubscriptionRenewalFlow, SubscriptionRenewalState>(
            id,
            state,
            options,
            cancellationToken);

        return FlowStartResults.ToActionResult(this, result);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(string id, [FromQuery] string? runId, CancellationToken cancellationToken)
    {
        ExecutionStatusInfo? status = string.IsNullOrWhiteSpace(runId)
            ? await _engine.GetStatusAsync<SubscriptionRenewalFlow>(id, cancellationToken)
            : await _engine.GetStatusAsync(
                typeof(SubscriptionRenewalFlow).FullName ?? nameof(SubscriptionRenewalFlow),
                id,
                runId,
                cancellationToken);

        return status is null ? NotFound() : Ok(status);
    }
}
