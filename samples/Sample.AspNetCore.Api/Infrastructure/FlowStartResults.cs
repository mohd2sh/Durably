using Microsoft.AspNetCore.Mvc;

namespace Sample.AspNetCore.Api.Infrastructure;

internal static class FlowStartResults
{
    public static IActionResult ToActionResult(ControllerBase controller, FlowStartResult result)
    {
        var body = new
        {
            outcome = result.Outcome.ToString(),
            flow = result.FlowName,
            instanceId = result.InstanceId,
            runId = result.RunId,
            status = result.Status.ToString()
        };

        return result.Outcome switch
        {
            FlowStartOutcome.Created => controller.Accepted(body),
            FlowStartOutcome.Skipped => controller.Ok(body),
            FlowStartOutcome.Conflict => controller.Conflict(body),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, body)
        };
    }
}
