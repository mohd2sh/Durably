using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Durably;

internal static class ExecutionsEndpoints
{
    public static RouteGroupBuilder MapExecutionsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet($"/{DurablyUIRoutes.Executions}", SearchExecutionsAsync);
        group.MapGet($"/{DurablyUIRoutes.Executions}/{{flowName}}/{{instanceId}}", GetExecutionAsync);
        group.MapGet($"/{DurablyUIRoutes.Executions}/{{flowName}}/{{instanceId}}/traces", GetTracesAsync);
        return group;
    }

    private static async Task<IResult> SearchExecutionsAsync(
        IExecutionQuery query,
        string? flowName,
        ExecutionStatus? status,
        string? instanceId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? metadataKey,
        string? metadataValue,
        int? skip,
        int? take,
        CancellationToken cancellationToken)
    {
        var criteria = new ExecutionSearchCriteria
        {
            FlowName = flowName,
            Status = status,
            InstanceId = instanceId,
            From = from,
            To = to,
            MetadataKey = metadataKey,
            MetadataValue = metadataValue,
            Skip = skip ?? 0,
            Take = take ?? QueryDefaults.DefaultPageSize
        };

        var result = await query.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetExecutionAsync(
        IExecutionQuery query,
        string flowName,
        string instanceId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowName) || string.IsNullOrWhiteSpace(instanceId))
        {
            return Results.BadRequest("Flow name and instance id are required.");
        }

        var detail = await query.GetAsync(flowName, instanceId, cancellationToken).ConfigureAwait(false);
        if (detail is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(detail);
    }

    private static async Task<IResult> GetTracesAsync(
        ITraceQuery traceQuery,
        string flowName,
        string instanceId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowName) || string.IsNullOrWhiteSpace(instanceId))
        {
            return Results.BadRequest("Flow name and instance id are required.");
        }

        var traces = await traceQuery.GetTracesAsync(flowName, instanceId, cancellationToken).ConfigureAwait(false);
        return Results.Ok(traces);
    }
}
