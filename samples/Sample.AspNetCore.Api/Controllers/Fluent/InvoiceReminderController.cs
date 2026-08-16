using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Sample.AspNetCore.Api.Infrastructure;
using Sample.AspNetCore.Api.Workflows.Fluent.InvoiceReminder;

namespace Sample.AspNetCore.Api.Controllers.Fluent;

[ApiController]
[Route("api/invoice-reminder")]
public sealed class InvoiceReminderController : ControllerBase
{
    private readonly IFlowEngine _engine;
    private readonly IFlowBuilder<InvoiceReminderState> _flow;

    public InvoiceReminderController(IFlowEngine engine, IFlowBuilder<InvoiceReminderState> flow)
    {
        _engine = engine;
        _flow = flow;
    }

    public sealed class InvoiceReminderRequest
    {
        public string CustomerEmail { get; set; } = "billing@example.com";

        public string CustomerPhone { get; set; } = "+15551212";

        public int DaysOverdue { get; set; } = 14;

        public string Channel { get; set; } = "email";

        public decimal AmountDue { get; set; } = 120m;
    }

    [HttpPost("{id}")]
    public async Task<IActionResult> Start(string id, [FromBody] InvoiceReminderRequest? request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Invoice id is required.");
        }

        request ??= new InvoiceReminderRequest();
        var state = new InvoiceReminderState
        {
            InvoiceId = id,
            CustomerEmail = request.CustomerEmail,
            CustomerPhone = request.CustomerPhone,
            DaysOverdue = request.DaysOverdue,
            Channel = string.IsNullOrWhiteSpace(request.Channel) ? "email" : request.Channel.ToLowerInvariant(),
            AmountDue = request.AmountDue > 0 ? request.AmountDue : 120m
        };

        var options = new FlowStartOptions
        {
            Metadata = new Dictionary<string, string>
            {
                ["invoiceId"] = id,
                ["channel"] = state.Channel,
                ["daysOverdue"] = state.DaysOverdue.ToString(CultureInfo.InvariantCulture),
                ["workflow"] = "InvoiceReminder",
                ["style"] = "Fluent"
            }
        };

        var result = await _engine.StartAsync(_flow, id, state, options, cancellationToken);
        return FlowStartResults.ToActionResult(this, result);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(string id, CancellationToken cancellationToken)
    {
        var status = await _engine.GetStatusAsync(_flow.Name, id, cancellationToken);
        return status is null ? NotFound() : Ok(status);
    }
}
