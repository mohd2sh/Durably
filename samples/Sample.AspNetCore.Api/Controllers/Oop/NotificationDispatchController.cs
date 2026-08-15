using Microsoft.AspNetCore.Mvc;
using Sample.AspNetCore.Api.Infrastructure;
using Sample.AspNetCore.Api.Workflows.Oop.NotificationDispatch;

namespace Sample.AspNetCore.Api.Controllers.Oop;

[ApiController]
[Route("api/notification-dispatch")]
public sealed class NotificationDispatchController : ControllerBase
{
    private readonly IFlowEngine _engine;

    public NotificationDispatchController(IFlowEngine engine)
    {
        _engine = engine;
    }

    public sealed class NotificationRequest
    {
        public string Priority { get; set; } = "normal";

        public string Channel { get; set; } = "email";

        public string Recipient { get; set; } = "user@example.com";

        public string Message { get; set; } = string.Empty;
    }

    [HttpPost("{id}")]
    public async Task<IActionResult> Start(string id, [FromBody] NotificationRequest? request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Notification id is required.");
        }

        request ??= new NotificationRequest();
        var state = new NotificationDispatchState
        {
            NotificationId = id,
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "normal" : request.Priority.ToLowerInvariant(),
            Channel = string.IsNullOrWhiteSpace(request.Channel) ? "email" : request.Channel.ToLowerInvariant(),
            Recipient = request.Recipient,
            Message = request.Message
        };

        var options = new FlowStartOptions
        {
            Metadata = new Dictionary<string, string>
            {
                ["notificationId"] = id,
                ["priority"] = state.Priority,
                ["channel"] = state.Channel,
                ["recipient"] = state.Recipient,
                ["workflow"] = "NotificationDispatch"
            }
        };

        var result = await _engine.StartAsync<NotificationDispatchFlow, NotificationDispatchState>(
            id,
            state,
            options,
            cancellationToken);

        return FlowStartResults.ToActionResult(this, result);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(string id, CancellationToken cancellationToken)
    {
        var status = await _engine.GetStatusAsync<NotificationDispatchFlow>(id, cancellationToken);
        return status is null ? NotFound() : Ok(status);
    }
}
