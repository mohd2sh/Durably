namespace Durably;

internal static class RoutePrefixNormalizer
{
    public static string Normalize(string routePrefix)
    {
        if (string.IsNullOrWhiteSpace(routePrefix))
        {
            return DurablyUIDefaults.RoutePrefix;
        }

        var trimmed = routePrefix.Trim();
        if (!trimmed.StartsWith('/'))
        {
            trimmed = $"/{trimmed}";
        }

        return trimmed.TrimEnd('/');
    }
}
