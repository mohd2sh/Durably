using System.Text;
using Dapper;

namespace Durably;

internal sealed class ExecutionSearchSqlBuilder
{
    private readonly ISqlDialect _dialect;

    public ExecutionSearchSqlBuilder(ISqlDialect dialect)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    }

    public (string Sql, DynamicParameters Parameters) BuildSearch(ExecutionSearchCriteria criteria)
    {
        var take = NormalizeTake(criteria.Take);
        var skip = Math.Max(0, criteria.Skip);
        var parameters = new DynamicParameters();
        var where = BuildWhereClause(criteria, parameters);

        var sql = new StringBuilder();
        sql.Append("SELECT ").Append(_dialect.SearchSelectColumns)
            .Append(" FROM ").Append(_dialect.ExecutionsTableName);
        sql.Append(where);
        sql.Append(" ORDER BY ").Append(_dialect.QuoteColumn("UpdatedAt")).Append(" DESC");
        parameters.Add("Skip", skip);
        parameters.Add("Take", take);
        sql.Append(_dialect.PagingClause);

        return (sql.ToString(), parameters);
    }

    public (string Sql, DynamicParameters Parameters) BuildCount(ExecutionSearchCriteria criteria)
    {
        var parameters = new DynamicParameters();
        var where = BuildWhereClause(criteria, parameters);
        var sql = $"SELECT COUNT(1) FROM {_dialect.ExecutionsTableName}{where}";
        return (sql, parameters);
    }

    private string BuildWhereClause(ExecutionSearchCriteria criteria, DynamicParameters parameters)
    {
        var clauses = new List<string> { DurablyLimits.SqlAlwaysTruePredicate };

        if (!string.IsNullOrWhiteSpace(criteria.FlowName))
        {
            var flowName = criteria.FlowName!;
            clauses.Add($"{_dialect.QuoteColumn("FlowName")} LIKE @FlowNamePattern");
            parameters.Add("FlowNamePattern", $"%{flowName.Trim()}%");
        }

        if (criteria.Status is not null)
        {
            clauses.Add($"{_dialect.QuoteColumn("Status")} = @Status");
            parameters.Add("Status", (int)criteria.Status);
        }

        if (!string.IsNullOrWhiteSpace(criteria.InstanceId))
        {
            var instanceId = criteria.InstanceId!;
            clauses.Add($"{_dialect.QuoteColumn("InstanceId")} LIKE @InstanceIdPattern");
            parameters.Add("InstanceIdPattern", $"%{instanceId.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(criteria.RunId))
        {
            var runId = criteria.RunId!;
            clauses.Add($"{_dialect.QuoteColumn("RunId")} LIKE @RunIdPattern");
            parameters.Add("RunIdPattern", $"%{runId.Trim()}%");
        }

        if (criteria.From is not null)
        {
            clauses.Add($"{_dialect.QuoteColumn("UpdatedAt")} >= @From");
            parameters.Add("From", criteria.From.Value.UtcDateTime);
        }

        if (criteria.To is not null)
        {
            clauses.Add($"{_dialect.QuoteColumn("UpdatedAt")} <= @To");
            parameters.Add("To", criteria.To.Value.UtcDateTime);
        }

        AppendMetadataFilter(criteria, clauses, parameters);

        return " WHERE " + string.Join(" AND ", clauses);
    }

    private void AppendMetadataFilter(
        ExecutionSearchCriteria criteria,
        List<string> clauses,
        DynamicParameters parameters)
    {
        if (string.IsNullOrWhiteSpace(criteria.MetadataKey)
            || string.IsNullOrWhiteSpace(criteria.MetadataValue))
        {
            return;
        }

        var metadataKey = criteria.MetadataKey!;
        var metadataValue = criteria.MetadataValue!;
        var bag = new Dictionary<string, object>(StringComparer.Ordinal);
        var predicate = _dialect.BuildMetadataEqualsPredicate(
            metadataKey.Trim(),
            metadataValue.Trim(),
            bag);
        if (predicate is null)
        {
            return;
        }

        foreach (var pair in bag)
        {
            parameters.Add(pair.Key, pair.Value);
        }

        clauses.Add(predicate);
    }

    private static int NormalizeTake(int take)
    {
        if (take <= 0)
        {
            return QueryDefaults.DefaultPageSize;
        }

        return Math.Min(take, QueryDefaults.MaxPageSize);
    }
}
