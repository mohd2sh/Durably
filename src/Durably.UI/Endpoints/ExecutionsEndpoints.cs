using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Durably;

internal static class ExecutionsEndpoints
{
    public static IReadOnlyList<IEndpointConventionBuilder> MapExecutionsEndpoints(
        this IEndpointRouteBuilder endpoints,
        string apiPath)
    {
        if (endpoints is null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        if (string.IsNullOrWhiteSpace(apiPath))
        {
            throw new ArgumentException("API path is required.", nameof(apiPath));
        }

        var root = $"{apiPath}/{DurablyUIRoutes.Executions}";
        return
        [
            endpoints.MapGet(root, SearchExecutionsAsync).ExcludeFromApiDescription(),
            endpoints.MapGet($"{root}/{{flowName}}/{{instanceId}}", GetExecutionAsync).ExcludeFromApiDescription(),
            endpoints.MapGet($"{root}/{{flowName}}/{{instanceId}}/traces", GetTracesAsync).ExcludeFromApiDescription()
        ];
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
