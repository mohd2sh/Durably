namespace Durably.Queries;
internal static class ExecutionSearchFilter
{
    public static IEnumerable<ExecutionRecord> Apply(IEnumerable<ExecutionRecord> source, ExecutionSearchCriteria criteria)
    {
        var query = source;

        if (!string.IsNullOrWhiteSpace(criteria.FlowName))
        {
            var flowName = criteria.FlowName!;
            query = query.Where(record => ContainsIgnoreCase(record.FlowName, flowName));
        }

        if (criteria.Status is not null)
        {
            query = query.Where(record => record.Status == criteria.Status);
        }

        if (!string.IsNullOrWhiteSpace(criteria.InstanceId))
        {
            var instanceId = criteria.InstanceId!;
            query = query.Where(record => ContainsIgnoreCase(record.InstanceId, instanceId));
        }

        if (criteria.From is not null)
        {
            query = query.Where(record => record.UpdatedAt >= criteria.From);
        }

        if (criteria.To is not null)
        {
            query = query.Where(record => record.UpdatedAt <= criteria.To);
        }

        if (!string.IsNullOrWhiteSpace(criteria.MetadataKey)
            && !string.IsNullOrWhiteSpace(criteria.MetadataValue))
        {
            var metadataKey = criteria.MetadataKey!;
            var metadataValue = criteria.MetadataValue!;
            query = query.Where(record => MetadataContains(record.MetadataJson, metadataKey, metadataValue));
        }

        return query.OrderByDescending(record => record.UpdatedAt);
    }

    private static bool ContainsIgnoreCase(string? source, string value)
    {
        if (source is null || source.Length == 0)
        {
            return false;
        }

        return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool MetadataContains(string? metadataJson, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return false;
        }

        return ContainsIgnoreCase(metadataJson, $"\"{key}\":\"{value}\"")
            || ContainsIgnoreCase(metadataJson, $"\"{key}\": \"{value}\"");
    }
}
