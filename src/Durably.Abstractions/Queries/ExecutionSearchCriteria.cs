namespace Durably.Queries;
/// <summary>Filters for listing persisted flow instances in the UI and query API.</summary>
public sealed class ExecutionSearchCriteria
{
    public string? FlowName { get; set; }

    public ExecutionStatus? Status { get; set; }

    public string? InstanceId { get; set; }

    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }

    public string? MetadataKey { get; set; }

    public string? MetadataValue { get; set; }

    public int Skip { get; set; }

    public int Take { get; set; } = QueryDefaults.DefaultPageSize;
}
